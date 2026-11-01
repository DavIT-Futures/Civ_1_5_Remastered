using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public abstract class VariableGroup
		{
			internal IEnumerable<GlobalWord> Words => GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(member => member.FieldType == typeof(GlobalWord))
				.Select(member => (GlobalWord)member.GetValue(this)!);
		}
	}
}
