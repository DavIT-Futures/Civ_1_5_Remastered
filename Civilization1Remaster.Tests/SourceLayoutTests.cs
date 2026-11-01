using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Civilization1Remaster.Tests
{
	public class SourceLayoutTests
	{
		[Fact]
		public void EveryFileHoldsOneType()
		{
			string[] crowded = SourceFiles()
				.Where(file => TypesWithContent(file).Count() > 1)
				.Select(file => $"{Path.GetRelativePath(GameFiles.Solution, file)}: {string.Join(", ", TypesWithContent(file))}")
				.ToArray();

			Assert.Empty(crowded);
		}

		[Fact]
		public void NoFileIsEmpty()
		{
			string[] empty = SourceFiles()
				.Where(file => File.ReadAllText(file).Trim('﻿', ' ', '\t', '\r', '\n').Length == 0)
				.Select(file => Path.GetRelativePath(GameFiles.Solution, file))
				.ToArray();

			Assert.Empty(empty);
		}

		private static IEnumerable<string> SourceFiles() =>
			Directory.EnumerateDirectories(GameFiles.Solution, "Civilization1Remaster.*")
				.SelectMany(project => Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
				.Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
				.Where(file => !file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase));

		private static IEnumerable<string> TypesWithContent(string file) =>
			CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
				.DescendantNodes()
				.OfType<MemberDeclarationSyntax>()
				.Where(HasOwnContent)
				.Select(type => type is BaseTypeDeclarationSyntax named ? named.Identifier.Text : ((DelegateDeclarationSyntax)type).Identifier.Text);

		private static bool HasOwnContent(MemberDeclarationSyntax member) => member switch
		{
			EnumDeclarationSyntax or DelegateDeclarationSyntax => true,
			TypeDeclarationSyntax type => type.ParameterList != null || type.Members.Count == 0 || type.Members.Any(inner => !IsType(inner)),
			_ => false,
		};

		private static bool IsType(MemberDeclarationSyntax member) => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax;
	}
}
