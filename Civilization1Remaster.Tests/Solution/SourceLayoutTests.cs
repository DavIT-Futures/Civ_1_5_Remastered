using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Civilization1Remaster.Tests.Solution
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

		[Fact]
		public void ProjectFoldersHoldOnlyTheProjectFile()
		{
			string[] loose = Projects()
				.Append(TestsFolder())
				.SelectMany(project => Directory.EnumerateFiles(project))
				.Where(file => !file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".csproj.user", StringComparison.OrdinalIgnoreCase))
				.Select(file => Path.GetRelativePath(GameFiles.Solution, file))
				.ToArray();

			Assert.Empty(loose);
		}

		[Fact]
		public void EveryProjectHasTestsOfItsOwn()
		{
			string[] untested = Projects()
				.Select(project => TestFolderOf(project))
				.Where(folder => !Directory.Exists(folder) || !Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).Any(HoldsTest))
				.Select(folder => Path.GetFileName(folder))
				.ToArray();

			Assert.Empty(untested);
		}

		[Fact]
		public void EveryTestFolderNamesAProject()
		{
			HashSet<string> expected = Projects().Select(project => Path.GetFileName(TestFolderOf(project))).Append("Solution").ToHashSet();

			string[] strays = Directory.EnumerateDirectories(TestsFolder())
				.Select(folder => Path.GetFileName(folder))
				.Where(name => name is not "bin" and not "obj" && !expected.Contains(name))
				.ToArray();

			Assert.Empty(strays);
		}

		private static string TestsFolder() => Path.Combine(GameFiles.Solution, "Civilization1Remaster.Tests");

		private static IEnumerable<string> Projects() =>
			Directory.EnumerateDirectories(GameFiles.Solution, "Civilization1Remaster.*")
				.Where(project => Path.GetFileName(project) != "Civilization1Remaster.Tests");

		private static string TestFolderOf(string project) =>
			Path.Combine(TestsFolder(), Path.GetFileName(project).Substring("Civilization1Remaster.".Length));

		private static bool HoldsTest(string file)
		{
			string text = File.ReadAllText(file);
			return text.Contains("[Fact]") || text.Contains("[Theory]");
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
