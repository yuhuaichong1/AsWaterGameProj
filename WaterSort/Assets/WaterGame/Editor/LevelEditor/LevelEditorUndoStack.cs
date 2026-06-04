using System.Collections.Generic;
using AsGame.Data;

namespace AsGame.Editor.LevelEditor
{
    sealed class LevelEditorSnapshot
    {
        public List<CupData> Cups { get; }
        public int SelectedCup { get; }

        public LevelEditorSnapshot(List<CupData> cups, int selectedCup)
        {
            Cups = CloneCups(cups);
            SelectedCup = selectedCup;
        }

        static List<CupData> CloneCups(List<CupData> source)
        {
            var list = new List<CupData>(source?.Count ?? 0);
            if (source == null) return list;
            for (var i = 0; i < source.Count; i++)
                list.Add(source[i].Clone());
            return list;
        }
    }

    sealed class LevelEditorUndoStack
    {
        readonly Stack<LevelEditorSnapshot> _undo = new();
        readonly Stack<LevelEditorSnapshot> _redo = new();
        const int MaxDepth = 64;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }

        public void Push(List<CupData> cups, int selectedCup)
        {
            _redo.Clear();
            _undo.Push(new LevelEditorSnapshot(cups, selectedCup));
            while (_undo.Count > MaxDepth)
            {
                var arr = _undo.ToArray();
                _undo.Clear();
                for (var i = arr.Length - 2; i >= 0; i--)
                    _undo.Push(arr[i]);
            }
        }

        public bool Undo(List<CupData> cups, ref int selectedCup)
        {
            if (_undo.Count == 0) return false;
            _redo.Push(new LevelEditorSnapshot(cups, selectedCup));
            var snap = _undo.Pop();
            Apply(snap, cups, ref selectedCup);
            return true;
        }

        public bool Redo(List<CupData> cups, ref int selectedCup)
        {
            if (_redo.Count == 0) return false;
            _undo.Push(new LevelEditorSnapshot(cups, selectedCup));
            var snap = _redo.Pop();
            Apply(snap, cups, ref selectedCup);
            return true;
        }

        static void Apply(LevelEditorSnapshot snap, List<CupData> cups, ref int selectedCup)
        {
            cups.Clear();
            cups.AddRange(snap.Cups);
            selectedCup = snap.SelectedCup;
            if (selectedCup >= cups.Count)
                selectedCup = cups.Count > 0 ? cups.Count - 1 : -1;
        }
    }
}
