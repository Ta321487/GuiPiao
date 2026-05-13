using GuiPiao.ViewModel.TrainTicketForm;
using Xunit;

namespace GuiPiao.Tests.ViewModel.TrainTicketForm;

public class UndoRedoManagerTests
{
    [Fact]
    public void Undo_恢复变更前字段_Redo_恢复变更后()
    {
        var data = new TrainTicketFormData { TrainNoNumber = "1" };
        var mgr = new UndoRedoManager();
        mgr.Initialize(10);
        mgr.SetCurrentData(data);
        mgr.StateRestored += (state, _) => state.ApplyTo(data);

        mgr.BeginPropertyChange(nameof(TrainTicketFormData.TrainNoNumber));
        data.TrainNoNumber = "2";
        Assert.True(mgr.CanUndo);
        Assert.False(mgr.CanRedo);

        mgr.Undo();
        Assert.Equal("1", data.TrainNoNumber);
        Assert.False(mgr.CanUndo);
        Assert.True(mgr.CanRedo);

        mgr.Redo();
        Assert.Equal("2", data.TrainNoNumber);
        Assert.True(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void Clear_清空栈()
    {
        var data = new TrainTicketFormData { TrainNoNumber = "a" };
        var mgr = new UndoRedoManager();
        mgr.Initialize(5);
        mgr.SetCurrentData(data);
        mgr.BeginPropertyChange(nameof(TrainTicketFormData.TrainNoNumber));
        data.TrainNoNumber = "b";
        mgr.Clear();
        Assert.False(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void FormState_ApplyTo_覆盖目标字段()
    {
        var src = new TrainTicketFormData { TrainNoNumber = "Z", DepartStationInput = "武汉" };
        var dst = new TrainTicketFormData { TrainNoNumber = "A", DepartStationInput = "北京" };
        var state = FormState.FromFormData(src, nameof(TrainTicketFormData.TrainNoNumber));
        state.ApplyTo(dst);
        Assert.Equal("Z", dst.TrainNoNumber);
        Assert.Equal("武汉", dst.DepartStationInput);
    }
}
