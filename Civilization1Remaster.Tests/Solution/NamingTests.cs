using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Civilization1Remaster.Tests.Solution
{
	public class NamingTests
	{
		private static readonly HashSet<string> Abbreviations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"acc", "addr", "adj", "ai", "app", "arg", "args", "attr", "buf", "calc", "cfg", "char", "chr", "civ", "cnt", "cpu", "crc", "cur",
			"dest", "diff", "dir", "dist", "dos", "dst", "ega", "cga", "elem", "fm", "gfx", "glob", "hnd", "horiz", "id", "idx", "imp", "info",
			"init", "irq", "jdx", "kdx", "len", "lhs", "loc", "lzw", "max", "min", "msg", "num", "obj", "opl", "pal", "pic", "pos", "prev",
			"ptr", "quot", "reg", "req", "ret", "rhs", "rle", "seg", "spec", "src", "str", "synth", "tech", "temp", "tmp", "txt", "val", "var",
			"vert", "vga",
		};

		private static readonly HashSet<string> Required = new HashSet<string>
		{
			"BuildAvaloniaApp", "obj",
		};

		[Fact]
		public void NamesHaveNoDigitsNorAbbreviations()
		{
			string[] wrong = SourceFiles()
				.SelectMany(file => DeclaredNames(file).Select(name => (File: file, Name: name)))
				.Where(declared => !Required.Contains(declared.Name) && IsWrong(declared.Name))
				.Select(declared => $"{Path.GetFileName(declared.File)}: {declared.Name}")
				.Distinct()
				.ToArray();

			Assert.Empty(wrong);
		}

		private static bool IsWrong(string name) =>
			Regex.IsMatch(name, "[0-9]")
			|| (name.Length == 1 && name != "T")
			|| Regex.Matches(name, "[A-Z]+(?![a-z])|[A-Z]?[a-z]+").Any(word => Abbreviations.Contains(word.Value));

		private static IEnumerable<string> SourceFiles() =>
			Directory.EnumerateDirectories(GameFiles.Solution, "Civilization1Remaster.*")
				.SelectMany(project => Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
				.Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
				.Where(file => !file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase));

		private static IEnumerable<string> DeclaredNames(string file) =>
			CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
				.DescendantNodes()
				.Select(Identifier)
				.Where(token => token.HasValue && token.Value.Text.Length > 0 && token.Value.Text != "_")
				.Select(token => token!.Value.Text);

		private static SyntaxToken? Identifier(SyntaxNode node) => node switch
		{
			BaseTypeDeclarationSyntax type => type.Identifier,
			DelegateDeclarationSyntax callback => callback.Identifier,
			MethodDeclarationSyntax method => method.Identifier,
			PropertyDeclarationSyntax property => property.Identifier,
			EventDeclarationSyntax happening => happening.Identifier,
			VariableDeclaratorSyntax variable => variable.Identifier,
			ParameterSyntax parameter => parameter.Identifier,
			EnumMemberDeclarationSyntax member => member.Identifier,
			SingleVariableDesignationSyntax designation => designation.Identifier,
			ForEachStatementSyntax loop => loop.Identifier,
			LocalFunctionStatementSyntax function => function.Identifier,
			TypeParameterSyntax generic => generic.Identifier,
			CatchDeclarationSyntax caught => caught.Identifier,
			_ => null,
		};
	}
}
