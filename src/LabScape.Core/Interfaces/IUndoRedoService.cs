namespace LabScape.Core.Interfaces;

public interface IUndoableCommand
{
    string Description { get; }
    void Execute();
    void Unexecute();
}

public interface IUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void ExecuteCommand(IUndoableCommand command);
    void Undo();
    void Redo();
    void Clear();
    event EventHandler? StateChanged;
}
