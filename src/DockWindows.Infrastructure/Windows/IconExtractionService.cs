using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DockWindows.Core.Models;
using Microsoft.Win32;

namespace DockWindows.Infrastructure.Windows;

public interface IIconExtractionService
{
    ImageSource? ObterIcone(ItemFixado item);
    ImageSource? ObterIcone(string caminhoOuUrl, TipoItem tipo = TipoItem.Aplicativo);
    ImageSource? ObterIconeJanela(IntPtr hWnd);

    /// <summary>
    /// Ícone oficial de um app empacotado (Loja do Windows) dono da janela, via AUMID.
    /// Retorna null se a janela pertence a um programa clássico.
    /// </summary>
    ImageSource? ObterIconeAppModernoJanela(IntPtr hWnd);
}

public class IconExtractionService : IIconExtractionService
{
    private static readonly ConcurrentDictionary<string, ImageSource?> CacheIcones = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Collections.Generic.Queue<(string Chave, long Bytes)> OrdemCache = new();
    private static readonly object CacheLock = new();
    private const int LimiteEntradas = 128;
    private const long LimiteBytes = 8 * 1024 * 1024;
    private static long _bytesCache;
    public static int QuantidadeCache => CacheIcones.Count;
    public static long BytesEstimadosCache { get { lock (CacheLock) return _bytesCache; } }

    private static bool GuardarIcone(string chave, ImageSource? imagem)
    {
        if (imagem == null) return false;
        // Orçamento dos pixels, não do arquivo comprimido; exclui overhead WPF/GPU.
        long bytes = imagem is BitmapSource bitmap
            ? (long)((bitmap.PixelWidth * (long)bitmap.Format.BitsPerPixel + 7) / 8) * bitmap.PixelHeight
            : 64 * 1024;
        if (bytes <= 0 || bytes > LimiteBytes) return false;
        lock (CacheLock)
        {
            if (CacheIcones.ContainsKey(chave)) return false;
            while ((CacheIcones.Count >= LimiteEntradas || _bytesCache + bytes > LimiteBytes) && OrdemCache.Count > 0)
            {
                var antiga = OrdemCache.Dequeue();
                if (CacheIcones.TryRemove(antiga.Chave, out _)) _bytesCache -= antiga.Bytes;
            }
            if (!CacheIcones.TryAdd(chave, imagem)) return false;
            OrdemCache.Enqueue((chave, bytes));
            _bytesCache += bytes;
            return true;
        }
    }

    // Constantes do Shell Win32
    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint SHGFI_SYSICONINDEX = 0x000004000;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

    private const int SHIL_LARGE = 0x0;       // 32x32
    private const int SHIL_SMALL = 0x1;       // 16x16
    private const int SHIL_EXTRALARGE = 0x2;  // 48x48
    private const int SHIL_SYSSMALL = 0x3;    // 16x16
    private const int SHIL_JUMBO = 0x4;       // 256x256 (Ultra Alta Resolução)
    private const int ILD_TRANSPARENT = 0x1;

    private const uint WM_GETICON = 0x007F;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const int ICON_SMALL2 = 2;
    private const int GCLP_HICON = -14;
    private const int GCLP_HICONSM = -34;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SHGetImageList(int iImageList, ref Guid riid, out IntPtr ppv);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", EntryPoint = "PrivateExtractIconsW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint PrivateExtractIcons(
        string lpszFile,
        int nIconIndex,
        int cxIcon,
        int cyIcon,
        IntPtr[] phicon,
        uint[] piconid,
        uint nIcons,
        uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    [DllImport("user32.dll", EntryPoint = "GetClassLong")]
    private static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
    private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);

    private static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetClassLongPtr64(hWnd, nIndex) : (IntPtr)GetClassLong32(hWnd, nIndex);
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, StringBuilder text, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    public ImageSource? ObterIcone(ItemFixado item)
    {
        if (item.Tipo == TipoItem.WebUrl)
        {
            var browserExe = ObterCaminhoNavegadorPadrao();
            if (!string.IsNullOrEmpty(browserExe))
            {
                if (CacheIcones.TryGetValue(browserExe, out var iconBrowser))
                {
                    return iconBrowser;
                }
                var ib = ExtrairIconeAltaResolucao(browserExe, TipoItem.Aplicativo);
                if (ib != null)
                {
                    GuardarIcone(browserExe, ib);
                    return ib;
                }
            }
            return null;
        }

        if (item.CaminhoOuUrl.StartsWith(InstalledAppsScanner.PrefixoAppsFolder, StringComparison.OrdinalIgnoreCase))
        {
            var chave = item.CaminhoOuUrl;
            if (CacheIcones.TryGetValue(chave, out var iconShell)) return iconShell;
            var extraido = ObterIconeShellItem(chave, 128);
            if (extraido != null) GuardarIcone(chave, extraido);
            return extraido;
        }

        var caminhoResolvido = ResolverCaminhoCompleto(item.CaminhoOuUrl);
        if (CacheIcones.TryGetValue(caminhoResolvido, out var iconExistente))
        {
            return iconExistente;
        }

        // Atalhos podem declarar um ícone diferente do executável de destino.
        // IShellItemImageFactory respeita essa identidade visual do próprio .lnk.
        if (File.Exists(caminhoResolvido) &&
            caminhoResolvido.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            var iconAtalho = ObterIconeShellItem(caminhoResolvido, 128);
            if (iconAtalho != null)
            {
                GuardarIcone(caminhoResolvido, iconAtalho);
                return iconAtalho;
            }
        }

        var iconExtraido = ExtrairIconeAltaResolucao(caminhoResolvido, item.Tipo);
        if (iconExtraido != null)
        {
            GuardarIcone(caminhoResolvido, iconExtraido);
        }
        return iconExtraido;
    }

    public ImageSource? ObterIcone(string caminhoOuUrl, TipoItem tipo = TipoItem.Aplicativo)
    {
        return ObterIcone(new ItemFixado { CaminhoOuUrl = caminhoOuUrl, Tipo = tipo });
    }

    public ImageSource? ObterIconeAppModernoJanela(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return null;
        try
        {
            var aumid = ObterAumidJanela(hWnd);
            if (string.IsNullOrEmpty(aumid)) return null;

            var chave = InstalledAppsScanner.PrefixoAppsFolder + aumid;
            if (CacheIcones.TryGetValue(chave, out var existente) && existente != null) return existente;

            var icone = ObterIconeShellItem(chave, 128);
            if (icone != null) GuardarIcone(chave, icone);
            return icone;
        }
        catch
        {
            return null;
        }
    }

    public ImageSource? ObterIconeJanela(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return null;

        // 0. Apps da Loja do Windows: o ícone oficial do pacote é muito melhor que WM_GETICON
        var iconeModerno = ObterIconeAppModernoJanela(hWnd);
        if (iconeModerno != null) return iconeModerno;

        try
        {
            IntPtr hIcon = IntPtr.Zero;

            // 1. Tentar ícone grande via WM_GETICON com timeout seguro
            SendMessageTimeout(hWnd, WM_GETICON, (IntPtr)ICON_BIG, IntPtr.Zero, SMTO_ABORTIFHUNG, 150, out hIcon);

            // 2. Tentar ícone pequeno via WM_GETICON
            if (hIcon == IntPtr.Zero)
            {
                SendMessageTimeout(hWnd, WM_GETICON, (IntPtr)ICON_SMALL2, IntPtr.Zero, SMTO_ABORTIFHUNG, 150, out hIcon);
            }

            if (hIcon == IntPtr.Zero)
            {
                SendMessageTimeout(hWnd, WM_GETICON, (IntPtr)ICON_SMALL, IntPtr.Zero, SMTO_ABORTIFHUNG, 150, out hIcon);
            }

            // 3. Tentar ícone da classe de janela (GCLP_HICON)
            if (hIcon == IntPtr.Zero)
            {
                hIcon = GetClassLongPtr(hWnd, GCLP_HICON);
            }

            // 4. Tentar ícone pequeno da classe (GCLP_HICONSM)
            if (hIcon == IntPtr.Zero)
            {
                hIcon = GetClassLongPtr(hWnd, GCLP_HICONSM);
            }

            if (hIcon != IntPtr.Zero)
            {
                var bs = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }

            // 5. Fallback por executável do processo associado à janela
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != 0)
            {
                var caminhoExe = ObterCaminhoProcesso(pid);
                if (!string.IsNullOrEmpty(caminhoExe) && File.Exists(caminhoExe))
                {
                    return ObterIcone(caminhoExe, TipoItem.Aplicativo);
                }
            }
        }
        catch { }

        return null;
    }

    public static string ResolverCaminhoCompleto(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho)) return string.Empty;

        var exp = Environment.ExpandEnvironmentVariables(caminho).Trim('"', ' ');
        if (File.Exists(exp) || Directory.Exists(exp)) return exp;

        // Trata nomes relativos como explorer.exe, notepad.exe, cmd.exe, etc.
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var sysDir = Environment.SystemDirectory;

        var candidatos = new[]
        {
            Path.Combine(winDir, exp),
            Path.Combine(sysDir, exp),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), exp),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), exp),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", exp)
        };

        foreach (var c in candidatos)
        {
            if (File.Exists(c)) return c;
            if (!c.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(c + ".exe")) return c + ".exe";
        }

        // Busca no PATH do sistema
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var full = Path.Combine(dir.Trim(), exp);
                    if (File.Exists(full)) return full;
                    if (!full.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(full + ".exe")) return full + ".exe";
                }
                catch { }
            }
        }

        return exp;
    }

    private static ImageSource? ExtrairIconeAltaResolucao(string caminho, TipoItem tipo)
    {
        try
        {
            // 0. Executável de app da Loja (pasta WindowsApps protegida): usa o ícone oficial do pacote
            if (tipo == TipoItem.Aplicativo && caminho.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
            {
                var aumid = InstalledAppsScanner.ObterAumidPorCaminhoPacote(caminho);
                if (!string.IsNullOrEmpty(aumid))
                {
                    var iconPacote = ObterIconeShellItem(InstalledAppsScanner.PrefixoAppsFolder + aumid, 128);
                    if (iconPacote != null) return iconPacote;
                }
            }
            // 1. Para arquivos executáveis reais (.exe), tenta PrivateExtractIcons para obter os 256x256 nativos
            if (tipo == TipoItem.Aplicativo && File.Exists(caminho) && caminho.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var iconPe = ExtrairPorPrivateExtract(caminho, 256);
                if (iconPe != null) return iconPe;

                iconPe = ExtrairPorPrivateExtract(caminho, 48);
                if (iconPe != null) return iconPe;

                iconPe = ExtrairPorPrivateExtract(caminho, 32);
                if (iconPe != null) return iconPe;
            }

            // 2. Extração via Shell ImageList (SHIL_JUMBO 256x256 e SHIL_EXTRALARGE 48x48)
            var shinfo = new SHFILEINFO();
            uint flags = SHGFI_SYSICONINDEX;
            if (tipo == TipoItem.Pasta)
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                SHGetFileInfo(caminho, FILE_ATTRIBUTE_DIRECTORY, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            }
            else if (!File.Exists(caminho))
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                SHGetFileInfo(caminho, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            }
            else
            {
                SHGetFileInfo(caminho, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            }

            if (shinfo.iIcon >= 0)
            {
                // Tenta SHIL_JUMBO (256x256)
                var iconJumbo = ObterHIconPorImageList(shinfo.iIcon, SHIL_JUMBO);
                if (iconJumbo != null) return iconJumbo;

                // Tenta SHIL_EXTRALARGE (48x48)
                var iconExtraLarge = ObterHIconPorImageList(shinfo.iIcon, SHIL_EXTRALARGE);
                if (iconExtraLarge != null) return iconExtraLarge;

                // Tenta SHIL_LARGE (32x32)
                var iconLarge = ObterHIconPorImageList(shinfo.iIcon, SHIL_LARGE);
                if (iconLarge != null) return iconLarge;
            }

            // 3. Fallback com System.Drawing.Icon.ExtractAssociatedIcon (BCL)
            if (File.Exists(caminho))
            {
                try
                {
                    using var assocIcon = System.Drawing.Icon.ExtractAssociatedIcon(caminho);
                    if (assocIcon != null)
                    {
                        var bs = Imaging.CreateBitmapSourceFromHIcon(
                            assocIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        return bs;
                    }
                }
                catch { }
            }

            // 4. Fallback legado SHGetFileInfo SHGFI_ICON | SHGFI_LARGEICON
            shinfo = new SHFILEINFO();
            uint fallbackFlags = SHGFI_ICON | SHGFI_LARGEICON;
            if (tipo == TipoItem.Pasta)
            {
                SHGetFileInfo(caminho, FILE_ATTRIBUTE_DIRECTORY, ref shinfo, (uint)Marshal.SizeOf(shinfo), fallbackFlags);
            }
            else if (!File.Exists(caminho))
            {
                fallbackFlags |= SHGFI_USEFILEATTRIBUTES;
                SHGetFileInfo(caminho, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), fallbackFlags);
            }
            else
            {
                SHGetFileInfo(caminho, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), fallbackFlags);
            }

            if (shinfo.hIcon != IntPtr.Zero)
            {
                try
                {
                    var bs = Imaging.CreateBitmapSourceFromHIcon(
                        shinfo.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    return bs;
                }
                finally
                {
                    DestroyIcon(shinfo.hIcon);
                }
            }
        }
        catch { }

        return null;
    }

    private static ImageSource? ExtrairPorPrivateExtract(string caminhoExe, int tamanhoDesejado)
    {
        try
        {
            var hIcons = new IntPtr[1];
            var iconIds = new uint[1];
            uint total = PrivateExtractIcons(caminhoExe, 0, tamanhoDesejado, tamanhoDesejado, hIcons, iconIds, 1, 0);

            if (total > 0 && hIcons[0] != IntPtr.Zero)
            {
                try
                {
                    var bs = Imaging.CreateBitmapSourceFromHIcon(
                        hIcons[0],
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bs.Freeze();
                    return bs;
                }
                finally
                {
                    DestroyIcon(hIcons[0]);
                }
            }
        }
        catch { }

        return null;
    }

    private static ImageSource? ObterHIconPorImageList(int iconIndex, int iImageList)
    {
        try
        {
            var iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            int hr = SHGetImageList(iImageList, ref iidImageList, out IntPtr himl);
            if (hr == 0 && himl != IntPtr.Zero)
            {
                IntPtr hIcon = ImageList_GetIcon(himl, iconIndex, ILD_TRANSPARENT);
                if (hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var bs = Imaging.CreateBitmapSourceFromHIcon(
                            hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();
                        return bs;
                    }
                    finally
                    {
                        DestroyIcon(hIcon);
                    }
                }
            }
        }
        catch { }

        return null;
    }

    // ===================== Apps modernos (AUMID) e ícones do Shell =====================

    [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeSize size, int flags, out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int cx;
        public int cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBitmap
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public ushort bmPlanes;
        public ushort bmBitsPixel;
        public IntPtr bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBitmapInfoHeader
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeDibSection
    {
        public NativeBitmap dsBm;
        public NativeBitmapInfoHeader dsBmih;
        public uint dsBitfields0;
        public uint dsBitfields1;
        public uint dsBitfields2;
        public IntPtr dshSection;
        public uint dsOffset;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetApplicationUserModelId(IntPtr hProcess, ref uint applicationUserModelIdLength, StringBuilder? applicationUserModelId);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory? ppv);

    [DllImport("gdi32.dll", EntryPoint = "GetObject")]
    private static extern int GetObjectDib(IntPtr hgdiobj, int cbBuffer, ref NativeDibSection lpvObject);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    /// <summary>AUMID do processo dono da janela (null para programas clássicos não empacotados).</summary>
    private static string? ObterAumidJanela(IntPtr hWnd)
    {
        GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0) return null;

        var aumid = ObterAumidProcesso(pid);
        if (!string.IsNullOrEmpty(aumid)) return aumid;

        // Apps UWP rodam hospedados no ApplicationFrameHost: o processo real é dono da janela-filha
        var exe = ObterCaminhoProcesso(pid);
        if (!exe.EndsWith("ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase)) return null;

        string? encontrado = null;
        EnumChildWindows(hWnd, (filho, _) =>
        {
            GetWindowThreadProcessId(filho, out uint pidFilho);
            if (pidFilho != 0 && pidFilho != pid)
            {
                var a = ObterAumidProcesso(pidFilho);
                if (!string.IsNullOrEmpty(a))
                {
                    encontrado = a;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);

        return encontrado;
    }

    private static string? ObterAumidProcesso(uint pid)
    {
        IntPtr hProc = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
        if (hProc == IntPtr.Zero) return null;
        try
        {
            uint tamanho = 0;
            int r = GetApplicationUserModelId(hProc, ref tamanho, null);
            if (r != ERROR_INSUFFICIENT_BUFFER || tamanho == 0) return null;

            var sb = new StringBuilder((int)tamanho);
            r = GetApplicationUserModelId(hProc, ref tamanho, sb);
            return r == 0 ? sb.ToString() : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            CloseHandle(hProc);
        }
    }

    /// <summary>Ícone de qualquer item do Shell (ex.: "shell:AppsFolder\AUMID") via IShellItemImageFactory.</summary>
    private static ImageSource? ObterIconeShellItem(string parsingPath, int tamanho)
    {
        IShellItemImageFactory? fabrica = null;
        IntPtr hbm = IntPtr.Zero;
        try
        {
            var iid = typeof(IShellItemImageFactory).GUID;
            if (SHCreateItemFromParsingName(parsingPath, IntPtr.Zero, ref iid, out fabrica) != 0 || fabrica == null)
                return null;

            const int SIIGBF_BIGGERSIZEOK = 0x1;
            const int SIIGBF_ICONONLY = 0x4;
            var tamanhoNativo = new NativeSize { cx = tamanho, cy = tamanho };
            if (fabrica.GetImage(tamanhoNativo, SIIGBF_BIGGERSIZEOK | SIIGBF_ICONONLY, out hbm) != 0 || hbm == IntPtr.Zero)
                return null;

            return HBitmapParaImagem(hbm);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hbm != IntPtr.Zero) DeleteObject(hbm);
            if (fabrica != null && Marshal.IsComObject(fabrica)) Marshal.ReleaseComObject(fabrica);
        }
    }

    /// <summary>Converte HBITMAP 32bpp preservando o canal alfa (CreateBitmapSourceFromHBitmap o descarta).</summary>
    private static ImageSource? HBitmapParaImagem(IntPtr hbm)
    {
        var dib = new NativeDibSection();
        int tamanhoStruct = Marshal.SizeOf<NativeDibSection>();
        int lidos = GetObjectDib(hbm, tamanhoStruct, ref dib);

        if (lidos == tamanhoStruct && dib.dsBm.bmBits != IntPtr.Zero && dib.dsBm.bmBitsPixel == 32)
        {
            int largura = dib.dsBm.bmWidth;
            int altura = Math.Abs(dib.dsBm.bmHeight);
            int stride = dib.dsBm.bmWidthBytes;
            if (largura <= 0 || altura <= 0 || stride <= 0) return null;

            var buffer = new byte[stride * altura];
            Marshal.Copy(dib.dsBm.bmBits, buffer, 0, buffer.Length);

            // DIB "bottom-up" (altura positiva no cabeçalho) precisa ser invertido
            if (dib.dsBmih.biHeight > 0)
            {
                var linha = new byte[stride];
                for (int y = 0; y < altura / 2; y++)
                {
                    int topo = y * stride;
                    int baixo = (altura - 1 - y) * stride;
                    Buffer.BlockCopy(buffer, topo, linha, 0, stride);
                    Buffer.BlockCopy(buffer, baixo, buffer, topo, stride);
                    Buffer.BlockCopy(linha, 0, buffer, baixo, stride);
                }
            }

            bool temAlfa = false;
            for (int i = 3; i < buffer.Length; i += 4)
            {
                if (buffer[i] != 0) { temAlfa = true; break; }
            }
            if (!temAlfa)
            {
                for (int i = 3; i < buffer.Length; i += 4) buffer[i] = 255;
            }

            var bs = BitmapSource.Create(largura, altura, 96, 96, PixelFormats.Pbgra32, null, buffer, stride);
            bs.Freeze();
            return bs;
        }

        var fallback = Imaging.CreateBitmapSourceFromHBitmap(hbm, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        fallback.Freeze();
        return fallback;
    }

    private static string ObterCaminhoProcesso(uint pid)
    {
        IntPtr hProc = OpenProcess(0x1000, false, pid); // PROCESS_QUERY_LIMITED_INFORMATION
        if (hProc != IntPtr.Zero)
        {
            try
            {
                int size = 1024;
                var sb = new StringBuilder(size);
                if (QueryFullProcessImageName(hProc, 0, sb, ref size))
                {
                    return sb.ToString();
                }
            }
            finally
            {
                CloseHandle(hProc);
            }
        }
        return string.Empty;
    }

    private static string? ObterCaminhoNavegadorPadrao()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
            var progId = key?.GetValue("ProgId")?.ToString();
            if (!string.IsNullOrEmpty(progId))
            {
                using var cmdKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
                var cmd = cmdKey?.GetValue("")?.ToString();
                if (!string.IsNullOrEmpty(cmd))
                {
                    var match = Regex.Match(cmd, @"(?:""(?<path>[^""]+)""|(?<path>[^\s]+))");
                    if (match.Success)
                    {
                        var p = match.Groups["path"].Value;
                        if (File.Exists(p)) return p;
                    }
                }
            }
        }
        catch { }

        // Fallback para navegadores comuns instalados
        var edge = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe");
        if (File.Exists(edge)) return edge;
        edge = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft\Edge\Application\msedge.exe");
        if (File.Exists(edge)) return edge;

        var chrome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Google\Chrome\Application\chrome.exe");
        if (File.Exists(chrome)) return chrome;
        chrome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Google\Chrome\Application\chrome.exe");
        if (File.Exists(chrome)) return chrome;

        return null;
    }
}
