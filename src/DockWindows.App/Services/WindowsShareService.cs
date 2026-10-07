using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinRT;

namespace DockWindows.App.Services;

public sealed class WindowsShareService : IDisposable
{
    private static readonly Guid DataTransferManagerIid = new("A5CAEE9B-8708-49D1-8D36-67D25A8DA00C");
    private DataTransferManager? _manager;
    private IDataTransferManagerInterop? _interop;
    private string? _caminho;

    public bool Compartilhar(Window janela, string caminho, out string? erro)
    {
        erro = null;
        if (!File.Exists(caminho)) { erro = "O arquivo foi removido ou movido."; return false; }
        try
        {
            var hwnd = new WindowInteropHelper(janela).Handle;
            if (hwnd == IntPtr.Zero)
            {
                erro = "A janela da GigaDock ainda não está pronta para compartilhar.";
                return false;
            }
            _interop ??= DataTransferManager.As<IDataTransferManagerInterop>();
            if (_manager == null)
            {
                var iid = DataTransferManagerIid;
                var abi = _interop.GetForWindow(hwnd, ref iid);
                _manager = MarshalInterface<DataTransferManager>.FromAbi(abi);
                _manager.DataRequested += AoSolicitarDados;
            }
            _caminho = caminho;
            _interop.ShowShareUIForWindow(hwnd);
            return true;
        }
        catch (Exception ex) { erro = $"O painel Compartilhar não pôde ser aberto: {ex.Message}"; return false; }
    }

    private async void AoSolicitarDados(DataTransferManager sender, DataRequestedEventArgs args)
    {
        var deferral = args.Request.GetDeferral();
        try
        {
            var caminho = _caminho;
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) { args.Request.FailWithDisplayText("O arquivo não está mais disponível."); return; }
            var arquivo = await StorageFile.GetFileFromPathAsync(caminho);
            args.Request.Data.Properties.Title = Path.GetFileName(caminho);
            args.Request.Data.Properties.Description = "Compartilhado pela GigaDock";
            args.Request.Data.SetStorageItems(new[] { arquivo });
        }
        catch { args.Request.FailWithDisplayText("Não foi possível preparar este arquivo para compartilhamento."); }
        finally { deferral.Complete(); }
    }

    public void Dispose()
    {
        if (_manager != null) _manager.DataRequested -= AoSolicitarDados;
        _manager = null; _interop = null;
    }

    [ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow(IntPtr appWindow, ref Guid riid);
        void ShowShareUIForWindow(IntPtr appWindow);
    }
}
