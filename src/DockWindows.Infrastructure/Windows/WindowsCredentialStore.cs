using System.Runtime.InteropServices;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public sealed class WindowsCredentialStore : ISecretStore
{
    private const uint Generic = 1, PersistLocalMachine = 2;
    public string? Ler(string alvo)
    {
        if (!CredRead(alvo, Generic, 0, out var ptr)) return null;
        try
        {
            var cred = Marshal.PtrToStructure<Credential>(ptr);
            return cred.Blob == IntPtr.Zero || cred.BlobSize == 0 ? string.Empty : Marshal.PtrToStringUni(cred.Blob, (int)cred.BlobSize / 2);
        }
        finally { CredFree(ptr); }
    }
    public bool Salvar(string alvo, string segredo)
    {
        var bytes = System.Text.Encoding.Unicode.GetBytes(segredo ?? string.Empty);
        var blob = Marshal.AllocCoTaskMem(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, blob, bytes.Length);
            var cred = new Credential { Type = Generic, TargetName = alvo, BlobSize = (uint)bytes.Length, Blob = blob, Persist = PersistLocalMachine, UserName = Environment.UserName };
            return CredWrite(ref cred, 0);
        }
        finally { Marshal.FreeCoTaskMem(blob); }
    }
    public bool Remover(string alvo) => CredDelete(alvo, Generic, 0);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential { public uint Flags, Type; public string TargetName; public string? Comment; public long LastWritten; public uint BlobSize; public IntPtr Blob; public uint Persist, AttributeCount; public IntPtr Attributes; public string? TargetAlias; public string UserName; }
    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredWrite(ref Credential credential, uint flags);
    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CredDelete(string target, uint type, uint flags);
    [DllImport("advapi32.dll")] private static extern void CredFree(IntPtr credential);
}
