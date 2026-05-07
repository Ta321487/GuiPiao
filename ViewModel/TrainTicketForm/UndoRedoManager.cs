using System;
using System.Collections.Generic;

namespace GuiPiao.ViewModel.TrainTicketForm;

/// <summary>
///     撤销重做管理器
/// </summary>
public class UndoRedoManager
{
    private readonly Stack<FormState> _redoStack = new();
    private readonly Stack<FormState> _undoStack = new();
    private TrainTicketFormData? _currentData;
    private FormState? _initialState;
    private int _maxSteps = 50;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event Action<FormState, bool>? StateRestored;
    public event Action<FormState>? StateSaved;

    /// <summary>
    ///     初始化管理器
    /// </summary>
    public void Initialize(int maxSteps)
    {
        _maxSteps = Math.Max(1, maxSteps);
    }

    /// <summary>
    ///     设置当前数据引用（用于自动保存状态）
    /// </summary>
    public void SetCurrentData(TrainTicketFormData data)
    {
        _currentData = data;
    }

    /// <summary>
    ///     设置初始状态
    /// </summary>
    public void SetInitialState(FormState state)
    {
        _initialState = state;
        _undoStack.Clear();
        _redoStack.Clear();
    }

    /// <summary>
    ///     开始属性变更 - 保存变更前的状态
    /// </summary>
    public void BeginPropertyChange(string propertyName)
    {
        if (_currentData == null) return;

        // 保存变更前的状态
        var state = FormState.FromFormData(_currentData.Clone(), propertyName);
        SaveStateInternal(state);
    }

    /// <summary>
    ///     保存状态（内部方法）
    /// </summary>
    private void SaveStateInternal(FormState state)
    {
        _undoStack.Push(state);
        _redoStack.Clear();

        // 限制撤销步数
        while (_undoStack.Count > _maxSteps)
        {
            // 移除最旧的状态（栈底元素）
            var tempStack = new Stack<FormState>();
            var bottomItem = _undoStack.Pop(); // 先弹出栈顶
            while (_undoStack.Count > 1) tempStack.Push(_undoStack.Pop());
            if (_undoStack.Count > 0) _undoStack.Pop(); // 移除最旧的
            while (tempStack.Count > 0) _undoStack.Push(tempStack.Pop());
            _undoStack.Push(bottomItem); // 放回栈顶
        }

        StateSaved?.Invoke(state);
    }

    /// <summary>
    ///     手动保存状态（兼容旧接口）
    /// </summary>
    public void SaveState(FormState state, string propertyName)
    {
        SaveStateInternal(state);
    }

    /// <summary>
    ///     撤销
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        // 保存当前状态到重做栈，以便重做时可以恢复
        if (_currentData != null)
        {
            var currentState = FormState.FromFormData(_currentData.Clone(), _undoStack.Peek().PropertyName);
            _redoStack.Push(currentState);
        }

        var state = _undoStack.Pop();
        StateRestored?.Invoke(state, true);
    }

    /// <summary>
    ///     重做
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        var state = _redoStack.Pop();
        // 将重做前的状态压入撤销栈
        if (_currentData != null)
        {
            var currentState = FormState.FromFormData(_currentData.Clone(), state.PropertyName);
            _undoStack.Push(currentState);
        }

        StateRestored?.Invoke(state, false);
    }

    /// <summary>
    ///     清除所有状态
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}