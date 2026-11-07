using LabScape.Core.Interfaces;

namespace LabScape.Infrastructure.Services;

public class UndoRedoService : IUndoRedoService
{
    private readonly LinkedList<IUndoableCommand> _undoList = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private const int MaxHistory = 100;

    public bool CanUndo => _undoList.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public void ExecuteCommand(IUndoableCommand command)
    {
        command.Execute();
        _undoList.AddLast(command);
        _redoStack.Clear();

        if (_undoList.Count > MaxHistory)
        {
            _undoList.RemoveFirst();
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var command = _undoList.Last!.Value;
        _undoList.RemoveLast();
        command.Unexecute();
        _redoStack.Push(command);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var command = _redoStack.Pop();
        command.Execute();
        _undoList.AddLast(command);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoList.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
