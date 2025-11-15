using Abcs.Utils;

namespace Abcs;

public class Program
{
	public static void Main()
	{
		string text = "Hello World!";
		int boxRows = -1;
		int boxCols = -1;
		int layer = -1;

		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.0F, 0.0F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.5F, 0.0F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 1.0F, 0.0F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.0F, 0.5F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.5F, 0.5F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 1.0F, 0.5F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.0F, 1.0F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.5F, 1.0F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 1.0F, 1.0F);
		CIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.DarkRed, 0.25F, 0.75F);
		CIO.Draw();
	}
}
