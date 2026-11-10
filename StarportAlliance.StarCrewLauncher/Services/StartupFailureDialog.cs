using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>日志与 XAML 就绪前的启动失败提示，详情可一键复制。</summary>
internal static class StartupFailureDialog
{
    private const int CopyButtonId = 1001;
    private const int CloseButtonId = 1002;
    private const uint AllowDialogCancellation = 0x0008;
    private const uint UseHIconMain = 0x0002;
    private const int ErrorIconId = 32513;

    public static void Show(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string details = exception.ToString();
        string content =
            $"{exception.GetType().Name}：{exception.Message}\n\n"
            + $"操作系统：{Environment.OSVersion.VersionString}";

        TaskDialogButton[] buttons =
        [
            new() { Id = CopyButtonId, Text = "复制错误信息" },
            new() { Id = CloseButtonId, Text = "关闭" },
        ];

        int buttonSize = Marshal.SizeOf<TaskDialogButton>();
        nint buttonsPtr = Marshal.AllocHGlobal(buttonSize * buttons.Length);
        try
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Marshal.StructureToPtr(buttons[i], buttonsPtr + (i * buttonSize), false);
            }

            TaskDialogConfig config = new()
            {
                Size = (uint)Marshal.SizeOf<TaskDialogConfig>(),
                Flags = AllowDialogCancellation | UseHIconMain,
                WindowTitle = "StarCrew Launcher",
                MainIcon = LoadIcon(0, ErrorIconId),
                MainInstruction = "文件已损坏，请重新安装 SCL。",
                Content = content,
                Buttons = buttonsPtr,
                ButtonCount = (uint)buttons.Length,
                DefaultButton = CopyButtonId,
                ExpandedInformation = details,
                ExpandedControlText = "详细信息",
                CollapsedControlText = "收起详细信息",
            };

            int result = TaskDialogIndirect(in config, out int button, out _, out _);
            if (result != 0)
            {
                _ = MessageBox(0, $"{content}\n\n{details}", "StarCrew Launcher", 0x10);
                return;
            }

            if (button == CopyButtonId)
            {
                CopyToClipboard(details);
            }
        }
        finally
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Marshal.DestroyStructure<TaskDialogButton>(buttonsPtr + (i * buttonSize));
            }

            Marshal.FreeHGlobal(buttonsPtr);
        }
    }

    private static void CopyToClipboard(string text)
    {
        DataPackage data = new();
        data.SetText(text);
        Clipboard.SetContent(data);
        Clipboard.Flush();
    }

    [DllImport("comctl32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int TaskDialogIndirect(
        in TaskDialogConfig config,
        out int button,
        out int radioButton,
        out int verification
    );

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern nint LoadIcon(nint instance, nint name);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int MessageBox(nint window, string text, string caption, uint type);

    // SDK 用 pshpack1.h 包裹这两个结构体，x64 下 TASKDIALOGCONFIG 是 160 字节；不打包会被判 E_INVALIDARG。
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TaskDialogConfig
    {
        public uint Size;
        public nint ParentWindow;
        public nint Instance;
        public uint Flags;
        public uint CommonButtons;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? WindowTitle;

        public nint MainIcon;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? MainInstruction;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Content;

        public uint ButtonCount;
        public nint Buttons;
        public int DefaultButton;
        public uint RadioButtonCount;
        public nint RadioButtons;
        public int DefaultRadioButton;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? VerificationText;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? ExpandedInformation;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? ExpandedControlText;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? CollapsedControlText;

        public nint FooterIcon;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Footer;

        public nint Callback;
        public nint CallbackData;
        public uint Width;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TaskDialogButton
    {
        public int Id;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Text;
    }
}
