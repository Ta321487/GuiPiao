using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System;
using GuiPiao.Model;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

public partial class OcrRecognizeTicketWindow : Window
{
    public OcrRecognizeTicketWindow()
    {
        InitializeComponent();

        var vm = new OcrRecognizeTicketViewModel();
        vm.RequestClose = () =>
        {
            try
            {
                DialogResult = vm.DialogConfirmed;
            }
            catch
            {
                // 非 ShowDialog 打开时不能设 DialogResult
            }

            Close();
        };
        DataContext = vm;

        Loaded += (_, _) =>
        {
            if (vm.IsPasteMode)
            {
                RawTextBox.Focus();
                Keyboard.Focus(RawTextBox);
            }
        };

        // 粘贴模式下：焦点不在文本框时 Ctrl+V 仍粘贴进文本区
        PreviewKeyDown += (_, e) =>
        {
            if (!vm.IsPasteMode) return;
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (!ReferenceEquals(Keyboard.FocusedElement, RawTextBox))
                {
                    RawTextBox.Focus();
                    vm.PasteFromClipboardCommand.Execute(null);
                    e.Handled = true;
                }
            }
        };
    }

    public TicketImportDraft? ResultDraft =>
        DataContext is OcrRecognizeTicketViewModel vm ? vm.ResultDraft : null;

    public IReadOnlyList<TicketImportDraft> ResultDrafts =>
        DataContext is OcrRecognizeTicketViewModel vm
            ? vm.ResultDrafts
            : Array.Empty<TicketImportDraft>();
}
