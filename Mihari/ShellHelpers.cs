using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;
using Windows.UI.Notifications;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Windows.Data.Xml.Dom;

namespace ShellHelpers
{
    public class Toast
    {
        public static void Show(string APP_ID, string title, string message)
        {
            var template = ToastTemplateType.ToastText02;
            var toastXml = ToastNotificationManager.GetTemplateContent(template);
            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(title));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode(message));
            var toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\",\"Show\":\"True\",}");
            var notifier = ToastNotificationManager.CreateToastNotifier(APP_ID);
            notifier.Show(new ToastNotification(toastXml));
        }
    }

    public class Shortcut
    {
        public static void Install(string APP_ID, string shortcutPath)
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            var newShortcut = (IShellLinkW)new CShellLink();

            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetPath(exePath));
            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcut.SetArguments(""));

            var newShortcutProperties = (IPropertyStore)newShortcut;

            using (var appId = new PropVariant(APP_ID))
            {
                ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.SetValue(SystemProperties.System.AppUserModel.ID, appId));
                ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutProperties.Commit());
            }

            var newShortcutSave = (IPersistFile)newShortcut;

            var dir = Path.GetDirectoryName(shortcutPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            ShellHelpers.ErrorHelper.VerifySucceeded(newShortcutSave.Save(shortcutPath, true));
        }
    }

    internal enum STGM : long
    {
        STGM_READ = 0x00000000L,
        STGM_WRITE = 0x00000001L,
        STGM_READWRITE = 0x00000002L,
        STGM_SHARE_DENY_NONE = 0x00000040L,
        STGM_SHARE_DENY_READ = 0x00000030L,
        STGM_SHARE_DENY_WRITE = 0x00000020L,
        STGM_SHARE_EXCLUSIVE = 0x00000010L,
        STGM_PRIORITY = 0x00040000L,
        STGM_CREATE = 0x00001000L,
        STGM_CONVERT = 0x00020000L,
        STGM_FAILIFTHERE = 0x00000000L,
        STGM_DIRECT = 0x00000000L,
        STGM_TRANSACTED = 0x00010000L,
        STGM_NOSCRATCH = 0x00100000L,
        STGM_NOSNAPSHOT = 0x00200000L,
        STGM_SIMPLE = 0x08000000L,
        STGM_DIRECT_SWMR = 0x00400000L,
        STGM_DELETEONRELEASE = 0x04000000L,
    }

    internal static class ShellIIDGuid
    {
        internal const string IShellLinkW = "000214F9-0000-0000-C000-000000000046";
        internal const string CShellLink = "00021401-0000-0000-C000-000000000046";
        internal const string IPersistFile = "0000010b-0000-0000-C000-000000000046";
        internal const string IPropertyStore = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99";
    }

    [ComImport,
    Guid(ShellIIDGuid.IShellLinkW),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellLinkW
    {
        UInt32 GetPath(
            [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
            int cchMaxPath,
            //ref _WIN32_FIND_DATAW pfd, 
            IntPtr pfd,
            uint fFlags);
        UInt32 GetIDList(out IntPtr ppidl);
        UInt32 SetIDList(IntPtr pidl);
        UInt32 GetDescription(
            [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
            int cchMaxName);
        UInt32 SetDescription(
            [MarshalAs(UnmanagedType.LPWStr)] string pszName);
        UInt32 GetWorkingDirectory(
            [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
            int cchMaxPath
            );
        UInt32 SetWorkingDirectory(
            [MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        UInt32 GetArguments(
            [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs,
            int cchMaxPath);
        UInt32 SetArguments(
            [MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        UInt32 GetHotKey(out short wHotKey);
        UInt32 SetHotKey(short wHotKey);
        UInt32 GetShowCmd(out uint iShowCmd);
        UInt32 SetShowCmd(uint iShowCmd);
        UInt32 GetIconLocation(
            [Out(), MarshalAs(UnmanagedType.LPWStr)] out StringBuilder pszIconPath,
            int cchIconPath,
            out int iIcon);
        UInt32 SetIconLocation(
            [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
            int iIcon);
        UInt32 SetRelativePath(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,
            uint dwReserved);
        UInt32 Resolve(IntPtr hwnd, uint fFlags);
        UInt32 SetPath(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport,
    Guid(ShellIIDGuid.IPersistFile),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistFile
    {
        UInt32 GetCurFile(
            [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile
        );
        UInt32 IsDirty();
        UInt32 Load(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            [MarshalAs(UnmanagedType.U4)] STGM dwMode);
        UInt32 Save(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            bool fRemember);
        UInt32 SaveCompleted(
            [MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    }
    [ComImport]
    [Guid(ShellIIDGuid.IPropertyStore)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        UInt32 GetCount([Out] out uint propertyCount);
        UInt32 GetAt([In] uint propertyIndex, out PropertyKey key);
        UInt32 GetValue([In] ref PropertyKey key, [Out] PropVariant pv);
        UInt32 SetValue([In] ref PropertyKey key, [In] PropVariant pv);
        UInt32 Commit();
    }


    [ComImport,
    Guid(ShellIIDGuid.CShellLink),
    ClassInterface(ClassInterfaceType.None)]
    internal class CShellLink { }

    public static class ErrorHelper
    {
        public static void VerifySucceeded(UInt32 hresult)
        {
            if (hresult > 1)
            {
                throw new Exception("Failed with HRESULT: " + hresult.ToString("X"));
            }
        }
    }
}