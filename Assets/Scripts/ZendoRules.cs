using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ZendoRuleFragment {
	public int variant;
	public abstract string GetDescription(bool plural = false);
	public ZendoRuleFragment[] children;
}

public abstract class ZendoRule : ZendoRuleFragment {
	public abstract bool Evaluate(ZendoSymbol[] g);
}

public abstract class ZendoPredicate : ZendoRuleFragment {
	public abstract bool CellValid(ZendoSymbol[] g, int cell);
}

public abstract class ZendoGroup : ZendoRuleFragment {
	public abstract List<List<int>> GetGroups(ZendoSymbol[] g);
}

public abstract class ZendoGroupPredicate : ZendoRuleFragment {
	public abstract bool GroupValid(ZendoSymbol[] g, List<int> cells);
}



public class DebugRule : ZendoRule {
	public override string GetDescription(bool plural) {
		return "There is a yellow square";
	}

	public override bool Evaluate(ZendoSymbol[] g) {
		for (int i = 0; i < g.Length; i++) {
			if (!g[i].empty && g[i].color == 1 && g[i].shape == 2)
				return true;
		}
		return false;
	}
}


// RULES


public class UniversalRule : ZendoRule {
	public override string GetDescription(bool plural) {
		return string.Format("Every {0} is {1}", children[0].GetDescription(false), children[1].GetDescription(false));
	}

	public override bool Evaluate(ZendoSymbol[] g) {
		ZendoPredicate noun = (ZendoPredicate)children[0];
		ZendoPredicate pred = (ZendoPredicate)children[1];

		for (int i = 0; i < g.Length; i++) {
			if (noun.CellValid(g, i) && !pred.CellValid(g, i))
				return false;
		}
		return true;
	}
}

public class ExistentialRule : ZendoRule {
	public override string GetDescription(bool plural) {
		return string.Format("There is a {0} that is {1}", children[0].GetDescription(false), children[1].GetDescription(false));
	}

	public override bool Evaluate(ZendoSymbol[] g) {
		ZendoPredicate noun = (ZendoPredicate)children[0];
		ZendoPredicate pred = (ZendoPredicate)children[1];

		for (int i = 0; i < g.Length; i++) {
			if (noun.CellValid(g, i) && pred.CellValid(g, i))
				return true;
		}
		return false;
	}
}

public class NegativeUniversalRule : ZendoRule {
	public override string GetDescription(bool plural) {
		return string.Format("No {0} is {1}", children[0].GetDescription(false), children[1].GetDescription(false));
	}

	public override bool Evaluate(ZendoSymbol[] g) {
		ZendoPredicate noun = (ZendoPredicate)children[0];
		ZendoPredicate pred = (ZendoPredicate)children[1];

		for (int i = 0; i < g.Length; i++) {
			if (noun.CellValid(g, i) && pred.CellValid(g, i))
				return false;
		}
		return true;
	}
}

public class GroupRule : ZendoRule {
	public override string GetDescription(bool plural) {
		return string.Format("{0} {1}", children[0].GetDescription(true), children[1].GetDescription(true));
	}

	public override bool Evaluate(ZendoSymbol[] g) {
		ZendoGroup groupFinder = (ZendoGroup)children[0];
		ZendoGroupPredicate pred = (ZendoGroupPredicate)children[1];

		List<List<int>> groups = groupFinder.GetGroups(g);

		foreach (List<int> e in groups) {
			if (e.Count > 0 && !pred.GroupValid(g, e)) return false;
		}
		return true;
	}
}

public class LineRule : ZendoRule {
	public override string GetDescription(bool plural) {
		return string.Format("There is a {0} filled with {1}", (variant == 0 ? "row" : "column"), children[0].GetDescription(true));
	}

	public override bool Evaluate(ZendoSymbol[] g) {
		ZendoPredicate noun = (ZendoPredicate)children[0];

		int d1, d2;

		if (variant == 0) {
			// there is a row that...
			d1 = 3;
			d2 = 1;
		} else {
			// there is a column that...
			d1 = 1;
			d2 = 3;
		}

		for (int i = 0; i < 3; i++) {
			bool valid = true;
			for (int j = 0; j < 3; j++) {
				if (!noun.CellValid(g, (i * d1) + (j * d2)))
					valid = false;
			}
			if (valid)
				return true;
		}
		return false;
	}
}


// NOUNS


public class ColorNoun : ZendoPredicate {
	public override string GetDescription(bool plural) {
		string suffix = plural ? " symbols" : " symbol";
		switch (variant) {
		case 0:
			return "red" + suffix;
		case 1:
			return "yellow" + suffix;
		case 2:
			return "blue" + suffix;
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return !g[cell].empty && g[cell].color == variant;
	}
}

public class ShapeNoun : ZendoPredicate {
	public override string GetDescription(bool plural) {
		string suffix = plural ? "s" : "";
		switch (variant) {
		case 0:
			return "circle" + suffix;
		case 1:
			return "triangle" + suffix;
		case 2:
			return "square" + suffix;
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return !g[cell].empty && g[cell].shape == variant;
	}
}

public class RowNoun : ZendoPredicate {
	public override string GetDescription(bool plural) {
		string prefix = plural ? "symbols " : "symbol ";
		switch (variant) {
		case 0:
			return prefix + "in the top row";
		case 1:
			return prefix + "in the middle row";
		case 2:
			return prefix + "in the bottom row";
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return !g[cell].empty && (cell / 3) == variant;
	}
}

public class ColumnNoun : ZendoPredicate {
	public override string GetDescription(bool plural) {
		string prefix = plural ? "symbols " : "symbol ";
		switch (variant) {
		case 0:
			return prefix + "in the left column";
		case 1:
			return prefix + "in the middle column";
		case 2:
			return prefix + "in the right column";
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return !g[cell].empty && (cell % 3) == variant;
	}
}

public class EmptyCellNoun : ZendoPredicate {
	public override string GetDescription(bool plural) {
		if (plural)
			return "blank cells";
		else
			return "blank cell";
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return g[cell].empty;
	}
}


// NOUN PREDICATES


public class ColorPredicate : ZendoPredicate {
	public override string GetDescription(bool plural) {
		switch (variant) {
		case 0:
			return "red";
		case 1:
			return "yellow";
		case 2:
			return "blue";
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return !g[cell].empty && g[cell].color == variant;
	}
}

public class ShapePredicate : ZendoPredicate {
	public override string GetDescription(bool plural) {
		string prefix = plural ? "" : "a ";
		string suffix = plural ? "s" : "";

		switch (variant) {
		case 0:
			return prefix + "circle" + suffix;
		case 1:
			return prefix + "triangle" + suffix;
		case 2:
			return prefix + "square" + suffix;
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return !g[cell].empty && g[cell].shape == variant;
	}
}

public class RowPredicate : ZendoPredicate {
	public override string GetDescription(bool plural) {
		switch (variant) {
		case 0:
			return "in the top row";
		case 1:
			return "in the middle row";
		case 2:
			return "in the bottom row";
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return (cell / 3) == variant;
	}
}

public class ColumnPredicate : ZendoPredicate {
	public override string GetDescription(bool plural) {
		switch (variant) {
		case 0:
			return "in the left column";
		case 1:
			return "in the middle column";
		case 2:
			return "in the right column";
		default:
			throw new IndexOutOfRangeException();
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		return (cell % 3) == variant;
	}
}

public class AdjacencyPredicate : ZendoPredicate {
	public override string GetDescription(bool plural) {
		if (plural) {
			return "next to " + children[0].GetDescription(true);
		} else {
			return "next to a " + children[0].GetDescription(false);
		}
	}

	public override bool CellValid(ZendoSymbol[] g, int cell) {
		ZendoPredicate noun = (ZendoPredicate)children[0];

		int row = cell / 3;
		int col = cell % 3;

		if (row > 0 && noun.CellValid(g, cell - 3))
			return true; 
		if (row < 2 && noun.CellValid(g, cell + 3))
			return true;
		if (col > 0 && noun.CellValid(g, cell - 1))
			return true;
		if (col < 2 && noun.CellValid(g, cell + 1))
			return true;

		return false;
	}
}


// GROUPS


public class ColorGroup : ZendoGroup {
	public override string GetDescription(bool plural) {
		return "Symbols of the same color";
	}

	public override List<List<int>> GetGroups(ZendoSymbol[] g) {
		List<List<int>> result = new List<List<int>>();

		for (int i = 0; i < 3; i++) {
			List<int> thisColor = new List<int>();

			for (int cell = 0; cell < g.Length; cell++) {
				if (!g[cell].empty && g[cell].color == i)
					thisColor.Add(cell);
			}

			result.Add(thisColor);
		}

		return result;
	}
}

public class ShapeGroup : ZendoGroup {
	public override string GetDescription(bool plural) {
		return "Symbols of the same shape";
	}

	public override List<List<int>> GetGroups(ZendoSymbol[] g) {
		List<List<int>> result = new List<List<int>>();

		for (int i = 0; i < 3; i++) {
			List<int> thisShape = new List<int>();

			for (int cell = 0; cell < g.Length; cell++) {
				if (!g[cell].empty && g[cell].shape == i)
					thisShape.Add(cell);
			}

			result.Add(thisShape);
		}

		return result;
	}
}

public class IdenticalGroup : ZendoGroup {
	public override string GetDescription(bool plural) {
		return "Identical symbols";
	}

	public override List<List<int>> GetGroups(ZendoSymbol[] g) {
		List<List<int>> result = new List<List<int>>();

		for (int i = 0; i < 3; i++) {
			for (int j = 0; j < 3; j++) {
				List<int> thisSymbol = new List<int>();

				for (int cell = 0; cell < g.Length; cell++) {
					if (!g[cell].empty && g[cell].color == i && g[cell].shape == j)
						thisSymbol.Add(cell);
				}

				result.Add(thisSymbol);
			}
		}

		return result;
	}
}

public class LineGroup : ZendoGroup {
	public override string GetDescription(bool plural) {
		if (variant == 0)
			return "Symbols in the same row";
		else
			return "Symbols in the same column";
	}

	public override List<List<int>> GetGroups(ZendoSymbol[] g) {
		List<List<int>> result = new List<List<int>>();

		int d1, d2;

		if (variant == 0) {
			// same row...
			d1 = 3;
			d2 = 1;
		} else {
			// same column...
			d1 = 1;
			d2 = 3;
		}

		for (int i = 0; i < 3; i++) {
			List<int> thisLine = new List<int>();

			for (int j = 0; j < 3; j++) {
				int cell = (i * d1) + (j * d2);
				if (!g[cell].empty)
					thisLine.Add(cell);
			}

			result.Add(thisLine);
		}

		return result;
	}
}

public class AdjacentGroup : ZendoGroup {
	public override string GetDescription(bool plural) {
		return "Adjacent symbols";
	}

	public override List<List<int>> GetGroups(ZendoSymbol[] g) {
		List<List<int>> result = new List<List<int>>();

		AddIfBothNonempty(g, result, 0, 1);
		AddIfBothNonempty(g, result, 1, 2);
		AddIfBothNonempty(g, result, 3, 4);
		AddIfBothNonempty(g, result, 4, 5);
		AddIfBothNonempty(g, result, 6, 7);
		AddIfBothNonempty(g, result, 7, 8);

		AddIfBothNonempty(g, result, 0, 3);
		AddIfBothNonempty(g, result, 1, 4);
		AddIfBothNonempty(g, result, 2, 5);
		AddIfBothNonempty(g, result, 3, 6);
		AddIfBothNonempty(g, result, 4, 7);
		AddIfBothNonempty(g, result, 5, 8);

		return result;
	}

	private void AddIfBothNonempty(ZendoSymbol[] g, List<List<int>> groups, int cell1, int cell2) {
		if (!g[cell1].empty && !g[cell2].empty) {
			List<int> l = new List<int>();
			l.Add(cell1);
			l.Add(cell2);
			groups.Add(l);
		}
	}
}


// GROUP PREDICATES


public class ColorGroupPredicate : ZendoGroupPredicate {
	public override string GetDescription(bool plural) {
		return "are the same color";
	}

	public override bool GroupValid(ZendoSymbol[] g, List<int> cells) {
		int color = g[cells[0]].color;

		foreach (int cell in cells) {
			if (g[cell].color != color)
				return false;
		}

		return true;
	}
}

public class ShapeGroupPredicate : ZendoGroupPredicate {
	public override string GetDescription(bool plural) {
		return "are the same shape";
	}

	public override bool GroupValid(ZendoSymbol[] g, List<int> cells) {
		int shape = g[cells[0]].shape;

		foreach (int cell in cells) {
			if (g[cell].shape != shape)
				return false;
		}

		return true;
	}
}

public class ColorDistinctGroupPredicate : ZendoGroupPredicate {
	public override string GetDescription(bool plural) {
		return "are different colors";
	}

	public override bool GroupValid(ZendoSymbol[] g, List<int> cells) {
		for (int i = 0; i < 3; i++) {
			int count = 0;

			foreach (int cell in cells) {
				if (g[cell].color == i)
					count++;
			}

			if (count > 1)
				return false;
		}

		return true;
	}
}

public class ShapeDistinctGroupPredicate : ZendoGroupPredicate {
	public override string GetDescription(bool plural) {
		return "are different shapes";
	}

	public override bool GroupValid(ZendoSymbol[] g, List<int> cells) {
		for (int i = 0; i < 3; i++) {
			int count = 0;

			foreach (int cell in cells) {
				if (g[cell].shape == i)
					count++;
			}

			if (count > 1)
				return false;
		}

		return true;
	}
}

public class ColorShapeGroupPredicate : ZendoGroupPredicate {
	public override string GetDescription(bool plural) {
		return "are the same color or shape";
	}

	public override bool GroupValid(ZendoSymbol[] g, List<int> cells) {
		int color = g[cells[0]].color;
		int shape = g[cells[0]].shape;

		bool sameColor = true;
		bool sameShape = true;

		foreach (int cell in cells) {
			if (g[cell].color != color)
				sameColor = false;
			if (g[cell].shape != shape)
				sameShape = false;
		}

		return (sameColor || sameShape);
	}
}

public class ContiguousGroupPredicate : ZendoGroupPredicate {
	public override string GetDescription(bool plural) {
		return "form connected clusters";
	}

	public override bool GroupValid(ZendoSymbol[] g, List<int> cells) {
		// Flood fill on shapes

		HashSet<int> toVisit = new HashSet<int>();
		Queue<int> floodQueue = new Queue<int>();

		foreach (int cell in cells)
			toVisit.Add(cell);

		floodQueue.Enqueue(cells[0]);

		while (floodQueue.Count > 0) {
			int pos = floodQueue.Dequeue();

			toVisit.Remove(pos);

			int row = pos / 3;
			int col = pos % 3;

			// continue BFS
			if (row > 0 && toVisit.Contains(pos - 3))
				floodQueue.Enqueue(pos - 3);
			if (row < 2 && toVisit.Contains(pos + 3))
				floodQueue.Enqueue(pos + 3);
			if (col > 0 && toVisit.Contains(pos - 1))
				floodQueue.Enqueue(pos - 1);
			if (col < 2 && toVisit.Contains(pos + 1))
				floodQueue.Enqueue(pos + 1);
		}

		return (toVisit.Count == 0);
	}
}