using CellReport.Core;
using System.Collections.Generic;
using System.IO;

namespace CellReport.Editor.Avalonia.Editing;

public sealed class TemplateHistory
{
	private const int MaxStates = 2000;

	private readonly List<MemoryStream> _states = new();
	private int _current = -1;

	public bool CanUndo => _current > 0;

	public bool CanRedo => _current >= 0 && _current < _states.Count - 1;

	public void Clear()
	{
		foreach (var state in _states)
			state.Dispose();

		_states.Clear();
		_current = -1;
	}

	public void Push(CellManager manager)
	{
		while (_states.Count - 1 > _current)
			RemoveAt(_states.Count - 1);

		while (_states.Count >= MaxStates)
			RemoveAt(0);

		var stream = new MemoryStream();
		new CellTemplate(manager).Save(stream, TemplateType.Package);
		_states.Add(stream);
		_current = _states.Count - 1;
	}

	public CellManager Undo()
	{
		_current--;
		return LoadCurrent();
	}

	public CellManager Redo()
	{
		_current++;
		return LoadCurrent();
	}

	private CellManager LoadCurrent()
	{
		var stream = _states[_current];
		stream.Position = 0;
		var template = new CellTemplate();
		template.Load(stream);
		return template.Manager;
	}

	private void RemoveAt(int index)
	{
		_states[index].Dispose();
		_states.RemoveAt(index);
	}
}
