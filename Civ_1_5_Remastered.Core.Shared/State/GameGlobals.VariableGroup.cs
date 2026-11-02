using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
		public abstract class VariableGroup
		{
			internal IEnumerable<GlobalWord> Words => GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(member => member.FieldType == typeof(GlobalWord))
				.Select(member => (GlobalWord)member.GetValue(this)!);
		}
	}
}
