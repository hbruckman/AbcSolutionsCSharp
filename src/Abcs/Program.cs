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

		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.0F, 0.0F, "#");
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.5F, 0.0F, "-|");
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 1.0F, 0.0F, ConsoleIO.SINGLE_BORDER_PARTS_WITH_CONNECTORS);
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.0F, 0.5F, ConsoleIO.DOUBLE_BORDER_PARTS);
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.5F, 0.5F, ConsoleIO.DOUBLE_BORDER_PARTS_WITH_CONNECTORS);
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 1.0F, 0.5F);
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.0F, 1.0F);
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 0.5F, 1.0F);
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.Blue, 1.0F, 1.0F);
		ConsoleIO.Box(text, boxRows, boxCols, layer, ConsoleColor.White, ConsoleColor.DarkRed, 0.25F, 0.75F);
		ConsoleIO.Draw();
	}
}
