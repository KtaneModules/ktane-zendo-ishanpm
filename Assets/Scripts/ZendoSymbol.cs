using System.Text;

public struct ZendoSymbol {
	int symbol;

	public ZendoSymbol(bool empty, int color, int shape) {
		if (empty)
			symbol = 0;
		else
			symbol = color + (shape * 3) + 1;
	}

	public ZendoSymbol(int color, int shape) : this(false, color, shape) {}

	public static ZendoSymbol Empty { get { return new ZendoSymbol(true, 0, 0); } }

	public int color {
		get { return symbol == 0 ? 0 : (symbol - 1) % 3; }
		set { symbol = value + (shape * 3) + 1; }
	}

	public int shape {
		get { return symbol == 0 ? 0 : (symbol - 1) / 3; }
		set { symbol = color + (value * 3) + 1; }
	}

	public bool empty {
		get { return symbol == 0; }
		set { symbol = (value ? 0 : 1); }
	}

	public static string GridToString(ZendoSymbol[] grid) {
		StringBuilder sb = new StringBuilder();

		if (grid.Length == 9) {
			for (int i = 0; i < grid.Length; i++) {
				if (grid[i].empty) {
					sb.Append("  ");
				} else {
					switch (grid[i].color) {
					case 0: sb.Append("r"); break;
					case 1: sb.Append("y"); break;
					case 2: sb.Append("b"); break;
					default: sb.Append("?"); break;
					}

					switch (grid[i].shape) {
					case 0: sb.Append("C"); break;
					case 1: sb.Append("T"); break;
					case 2: sb.Append("S"); break;
					default: sb.Append("?"); break;
					}
				}

				if (i%3 == 2) {
					if (i != 8) {
						// new row
						sb.Append("\n--+--+--\n");
					}
				} else {
					// new cell
					sb.Append("|");
				}
			}
		} else {
			// non-standard grid???
			return string.Format("ZendoSymbol[{0}]", grid.Length);
		}

		return sb.ToString();
	}
}
