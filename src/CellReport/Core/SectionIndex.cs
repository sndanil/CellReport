using System;
using System.Collections.Generic;

namespace CellReport.Core
{
	internal struct SectionLookupKey : IEquatable<SectionLookupKey>
	{
		public readonly string Name;
		public readonly string ObjectName;

		public SectionLookupKey(string name, string objectName)
		{
			Name = name ?? string.Empty;
			ObjectName = objectName ?? string.Empty;
		}

		public bool Equals(SectionLookupKey other)
		{
			return Name == other.Name && ObjectName == other.ObjectName;
		}

		public override bool Equals(object obj)
		{
			return obj is SectionLookupKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Name.GetHashCode() * 397) ^ ObjectName.GetHashCode();
			}
		}
	}

	internal static class SectionIndex
	{
		public static void Register(Dictionary<SectionLookupKey, TemplateSection> index, TemplateSection section)
		{
			if (index == null)
				throw new ArgumentNullException(nameof(index));
			if (section == null)
				throw new ArgumentNullException(nameof(section));

			if (string.IsNullOrEmpty(section.ObjectName))
			{
				index[new SectionLookupKey(section.Name, string.Empty)] = section;
				return;
			}

			index[new SectionLookupKey(section.Name, section.ObjectName)] = section;
			var tokens = section.ObjectName.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < tokens.Length; i++)
			{
				var token = tokens[i].Trim();
				if (token.Length > 0)
					index[new SectionLookupKey(section.Name, token)] = section;
			}
		}

		public static void Unregister(Dictionary<SectionLookupKey, TemplateSection> index, TemplateSection section)
		{
			if (index == null || section == null)
				return;

			var keysToRemove = new List<SectionLookupKey>();
			foreach (var pair in index)
			{
				if (ReferenceEquals(pair.Value, section))
					keysToRemove.Add(pair.Key);
			}

			for (int i = 0; i < keysToRemove.Count; i++)
				index.Remove(keysToRemove[i]);
		}

		public static TemplateSection Find(Dictionary<SectionLookupKey, TemplateSection> index, string name, string objectName, bool strict)
		{
			if (index == null)
				return null;

			TemplateSection section;
			if (index.TryGetValue(new SectionLookupKey(name, objectName ?? string.Empty), out section))
				return section;

			if (!strict && index.TryGetValue(new SectionLookupKey(name, string.Empty), out section))
				return section;

			return null;
		}

		public static void Rebuild(Dictionary<SectionLookupKey, TemplateSection> index, IEnumerable<TemplateSection> sections)
		{
			index.Clear();
			foreach (var section in sections)
				Register(index, section);
		}
	}
}
