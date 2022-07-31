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
	public abstract int[][] GetGroups(ZendoSymbol[] g);
}

public abstract class ZendoGroupPredicate : ZendoRuleFragment {
	public abstract bool GroupValid(ZendoSymbol[] g, int[][] groups);
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

public class ColorNoun : ZendoPredicate {
	public override string GetDescription(bool plural) {
		string suffix = plural ? " shapes" : " shape";
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