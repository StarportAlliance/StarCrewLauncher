using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>日志与 XAML 就绪前的启动失败提示，详情可一键复制。</summary>
internal static class StartupFailureDialog
{
    private const int CopyButtonId = 1001;
    private const int CloseButtonId = 1002;
    private const uint AllowDialogCancellation = 0x0008;
    private const nint ErrorIcon = -2;

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
                Flags = AllowDialogCancellation,
                WindowTitle = "StarCrew Launcher 启动失败",
                MainIcon = ErrorIcon,
                MainInstruction = "启动器无法完成初始化",
                Content = content,
                Buttons = buttonsPtr,
                ButtonCount = (uint)buttons.Length,
                DefaultButton = CopyButtonId,
                ExpandedInformation = details,
                ExpandedControlText = "详细信息",
                CollapsedControlText = "收起详细信息",
            };

            int result = TaskDialogIndirect(in config, out int button, out _, out _);
            if (result == 0 && button == CopyButtonId)
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
    }

    [DllImport("comctl32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int TaskDialogIndirect(
        in TaskDialogConfig config,
        out int button,
        out int radioButton,
        out int verification
    );

    [StructLayout(LayoutKind.Sequential)]
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

    [StructLayout(LayoutKind.Sequential)]
    private struct TaskDialogButton
    {
        public int Id;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Text;
    }
}
