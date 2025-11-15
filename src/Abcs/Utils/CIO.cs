using System.Reflection.PortableExecutable;

namespace Abcs.Utils;

public class CIO
{
	public const string RESET = "\x1b[0m";

	public const string FG_DARK_GRAY = "\x1b[30m";
	public const string FG_DARK_RED = "\x1b[31m";
	public const string FG_DARK_GREEN = "\x1b[32m";
	public const string FG_DARK_YELLOW = "\x1b[33m";
	public const string FG_DARK_BLUE = "\x1b[34m";
	public const string FG_DARK_MAGENTA = "\x1b[35m";
	public const string FG_DARK_CYAN = "\x1b[36m";
	public const string FG_GRAY = "\x1b[37m";

	public const string FG_BLACK = "\x1b[90m";
	public const string FG_RED = "\x1b[91m";
	public const string FG_GREEN = "\x1b[92m";
	public const string FG_YELLOW = "\x1b[93m";
	public const string FG_BLUE = "\x1b[94m";
	public const string FG_MAGENTA = "\x1b[95m";
	public const string FG_CYAN = "\x1b[96m";
	public const string FG_WHITE = "\x1b[97m";

	public const string BG_BLACK = "\x1b[40m";
	public const string BG_DARK_RED = "\x1b[41m";
	public const string BG_DARK_GREEN = "\x1b[42m";
	public const string BG_DARK_YELLOW = "\x1b[43m";
	public const string BG_DARK_BLUE = "\x1b[44m";
	public const string BG_DARK_MAGENTA = "\x1b[45m";
	public const string BG_DARK_CYAN = "\x1b[46m";
	public const string BG_GRAY = "\x1b[47m";

	public const string BG_DARK_GRAY = "\x1b[100m";
	public const string BG_RED = "\x1b[101m";
	public const string BG_GREEN = "\x1b[102m";
	public const string BG_YELLOW = "\x1b[103m";
	public const string BG_BLUE = "\x1b[104m";
	public const string BG_MAGENTA = "\x1b[105m";
	public const string BG_CYAN = "\x1b[106m";
	public const string BG_WHITE = "\x1b[107m";

	public const int HL = 0;
	public const int VL = 1;
	public const int TL = 2;
	public const int TR = 3;
	public const int BL = 4;
	public const int BR = 5;
	public const int TC = 6;
	public const int BC = 7;
	public const int ML = 8;
	public const int MR = 9;

	public const string SINGLE_BORDER_PARTS = "─│┌┐└┘";
	public const string DOUBLE_BORDER_PARTS = "═║╔╗╚╝";

	public const string SINGLE_BORDER_PARTS_WITH_CONNECTORS = "─│┌┐└┘┴┬┤├";
	public const string DOUBLE_BORDER_PARTS_WITH_CONNECTORS = "═║╔╗╚╝╩╦╣╠";

	private static int rows = 25;
	private static int cols = 80;
	private static int layers = 1;

	private static int cursorRow = 0;
	private static int cursorCol = 0;
	private static int cursorLayer = 0;
	
	private static string[,,] buffer = new string[layers, rows, cols];

	public static void Set(int rows = -1, int cols = -1, int layers = -1)
	{
		CIO.cols = (cols < 0) ? CIO.cols : cols;
		CIO.rows = (rows < 0) ? CIO.rows : rows;
		CIO.layers = (layers < 0) ? CIO.layers : layers;
		buffer = new string[CIO.layers, rows, cols];		
	}

	public static void SetCols(int cols)
	{
		CIO.cols = (cols < 0) ? CIO.cols : cols;
		buffer = new string[layers, rows, CIO.cols];
	}

	public static void SetRows(int rows)
	{
		CIO.rows = (rows < 0) ? CIO.rows : rows;
		buffer = new string[layers, CIO.rows, cols];
	}

	public static void SetLayers(int layers)
	{
		CIO.layers = (layers < 0) ? CIO.layers : layers;
		buffer = new string[CIO.layers, rows, cols];
	}

	public static string[] GetBorder(string borderParts, ConsoleColor? fg, ConsoleColor? bg)
	{
		string[] border = new string[10];

		if(string.IsNullOrEmpty(borderParts))
		{
			Array.Fill(border, Color(string.Empty, fg, bg));
		}
		else if (borderParts.Length == 1)
		{
			Array.Fill(border, Color(borderParts, fg, bg));
		}
		else if(borderParts.Length >= 2)
		{
			string hl = Color(borderParts[HL].ToString(), fg, bg);
			string vl = Color(borderParts[VL].ToString(), fg, bg);

			Array.Fill(border, hl);
			border[VL] = vl;
			border[ML] = vl;
			border[MR] = vl;

			if(borderParts.Length >= 6)
			{
				border[TL] = Color(borderParts[TL].ToString(), fg, bg);
				border[TR] = Color(borderParts[TR].ToString(), fg, bg);
				border[BL] = Color(borderParts[BL].ToString(), fg, bg);
				border[BR] = Color(borderParts[BR].ToString(), fg, bg);
			}

			if(borderParts.Length >= 10)
			{
				border[TC] = Color(borderParts[TC].ToString(), fg, bg);
				border[BC] = Color(borderParts[BC].ToString(), fg, bg);
				border[ML] = Color(borderParts[ML].ToString(), fg, bg);
				border[MR] = Color(borderParts[MR].ToString(), fg, bg);
			}
		}

		return border;
	}

	public static void Write(string text, int row = -1, int col = -1, int layer = -1, ConsoleColor? fg = default, ConsoleColor? bg = default)
	{
		text = text.Replace("\r", "");

		if(row < 0) { row = cursorRow; }
		if(col < 0) { col = cursorCol; }
		if(layer < 0) { layer = cursorLayer; }

		cursorRow = row;
		cursorCol = col;
		cursorLayer = layer;

		for(int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			
			if(c == '\n' || cursorCol >= cols) { cursorRow++ ; cursorCol = 0; }

			buffer[layer, cursorRow, cursorCol++] = Color(c, fg, bg);
		}
	}

	public static void Box(string text, int boxRows = -1, int boxCols = -1, int layer = -1, ConsoleColor? fg = default, ConsoleColor? bg = default, float hAlign = 0.5F, float vAlign = 0.5F, string borderParts = SINGLE_BORDER_PARTS)
	{
		text = text.Replace("\r", "");

		var lines = Wrap(text, boxRows, boxCols);
		int maxRowCount = (boxRows > 0) ? boxRows : lines.Count;
		int maxColCount = (boxCols > 0) ? boxCols : lines.Max(l => l.Length);

		if(layer < 0) { layer = cursorLayer; }

		cursorLayer = layer;

		int l = (int)((cols - maxColCount - 2) * hAlign);
		int t = (int)((rows - maxRowCount - 2) * vAlign);
		int r = l + maxColCount + 1;
		int b = t + maxRowCount + 1;
		int c = (l+r)/2;
		int m = (t+b)/2;

		// Console.WriteLine($"text={text}");
		// Console.WriteLine($"text.Length={text.Length}");
		// Console.WriteLine($"lines={string.Join(", ", lines)}");
		// Console.WriteLine($"maxRowCount={maxRowCount}");
		// Console.WriteLine($"maxColCount={maxColCount}");
		// Console.WriteLine($"l={l}, t={t}, r={r}, b={b}, c={c}, m={m}");

		string[] bo = GetBorder(borderParts, fg, bg);

		for(int i=l+1; i<r; i++)
		{
			buffer[layer, t, i] = bo[HL];
			buffer[layer, b, i] = bo[HL];
		}

		for(int i=t+1; i<b; i++)
		{
			buffer[layer, i, l] = bo[VL];
			buffer[layer, i, r] = bo[VL];
		}

		buffer[layer, t, l] = bo[TL];
		buffer[layer, t, r] = bo[TR];
		buffer[layer, t, c] = bo[TC];
		buffer[layer, b, l] = bo[BL];
		buffer[layer, b, r] = bo[BR];
		buffer[layer, b, c] = bo[BC];
		buffer[layer, m, l] = bo[ML];
		buffer[layer, m, r] = bo[MR];

		for(int i=0; i<lines.Count; i++)
		{
			Write(lines[i], t+1+i, l+1, layer, fg, bg);
		}
	}

	public static List<string> Wrap(string text, int rowsLimit, int colsLimit)
	{
		int margin = (colsLimit < 0) ? cols - 2 : Math.Min(colsLimit, cols - 2);
		int lastIndex = 0;
		int colCount = 0;
		var lines = new List<string>();

		for(int i=0; i<text.Length; i++)
		{
			if(text[i] == '\n') {	lines.Add(text.Substring(lastIndex, i-lastIndex)); lastIndex = i + 1;	colCount = 0;	}
			else              	{	colCount++;	}
			if(colCount >= margin) {	lines.Add(text.Substring(lastIndex, i-lastIndex)); lastIndex = i;	colCount = 0;	}
		}

		lines.Add(text.Substring(lastIndex));

		return lines;
	}

	public static string ConsoleColorToAnsiForeground(ConsoleColor? color)
	{
		switch(color)
		{
			case ConsoleColor.Black: return FG_BLACK;
			case ConsoleColor.DarkRed: return FG_DARK_RED;
			case ConsoleColor.DarkGreen: return FG_DARK_GREEN;
			case ConsoleColor.DarkYellow: return FG_DARK_YELLOW;
			case ConsoleColor.DarkBlue: return FG_DARK_BLUE;
			case ConsoleColor.DarkMagenta: return FG_DARK_MAGENTA;
			case ConsoleColor.DarkCyan: return FG_DARK_CYAN;
			case ConsoleColor.DarkGray: return FG_DARK_GRAY;
			case ConsoleColor.Gray: return FG_GRAY;
			case ConsoleColor.Red: return FG_RED;
			case ConsoleColor.Green: return FG_GREEN;
			case ConsoleColor.Yellow: return FG_YELLOW;
			case ConsoleColor.Blue: return FG_BLUE;
			case ConsoleColor.Magenta: return FG_MAGENTA;
			case ConsoleColor.Cyan: return FG_CYAN;
			case ConsoleColor.White: return FG_WHITE;

			default:
				return RESET;
		}
	}

	public static string ConsoleColorToAnsiBackground(ConsoleColor? color)
	{
		switch(color)
		{
			case ConsoleColor.Black: return BG_BLACK;
			case ConsoleColor.DarkRed: return BG_DARK_RED;
			case ConsoleColor.DarkGreen: return BG_DARK_GREEN;
			case ConsoleColor.DarkYellow: return BG_DARK_YELLOW;
			case ConsoleColor.DarkBlue: return BG_DARK_BLUE;
			case ConsoleColor.DarkMagenta: return BG_DARK_MAGENTA;
			case ConsoleColor.DarkCyan: return BG_DARK_CYAN;
			case ConsoleColor.DarkGray: return BG_DARK_GRAY;
			case ConsoleColor.Gray: return BG_GRAY;
			case ConsoleColor.Red: return BG_RED;
			case ConsoleColor.Green: return BG_GREEN;
			case ConsoleColor.Yellow: return BG_YELLOW;
			case ConsoleColor.Blue: return BG_BLUE;
			case ConsoleColor.Magenta: return BG_MAGENTA;
			case ConsoleColor.Cyan: return BG_CYAN;
			case ConsoleColor.White: return BG_WHITE;

			default:
				return RESET;
		}
	}

	public static string Color(char ch, ConsoleColor? fg = null, ConsoleColor? bg = null)
	{
		string fgc = ConsoleColorToAnsiForeground(fg);
		string bgc = ConsoleColorToAnsiBackground(bg);

		return fgc + bgc + ch + "\x1b[0m";
	}

	public static string Color(string text, ConsoleColor? fg = null, ConsoleColor? bg = null)
	{
		string fgc = ConsoleColorToAnsiForeground(fg);
		string bgc = ConsoleColorToAnsiBackground(bg);

		return fgc + bgc + text + "\x1b[0m";
	}

	public static void Draw()
	{
		Console.SetWindowSize(cols + 1, rows + 1);
		Console.SetBufferSize(cols + 1, rows + 1);
		Console.Clear();
		Draw(buffer);
	}

	private static void Draw(string[,,] buffer)
	{
		for(int r = 0; r < rows; r++)
		{
			for(int c = 0; c < cols; c++)
			{
				string s = buffer[0, r, c];
				Console.Write(string.IsNullOrEmpty(s) ? " " : s);
			}

			Console.WriteLine();
		}
	}
}
