#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public static class WidgetUtilsCA
	{
		public static string WrapTextWithIndent(string text, int width, SpriteFont font, int indent = 4)
		{
			var textSize = font.Measure(text);
			var indentString = indent > 0 ? new string(' ', indent) : "";
			var effectiveWidth = indent > 0 ? width - font.Measure(indentString).X : width;

			if (textSize.X > width)
			{
				var lines = text.Split('\n').ToList();
				var isOriginalLine = new bool[lines.Count];

				// Mark all initial lines as original
				for (var i = 0; i < lines.Count; i++)
				{
					isOriginalLine[i] = true;
				}

				var isCjk = WidgetUtils.IsCurrentLanguageCjk();

				for (var i = 0; i < lines.Count; i++)
				{
					var line = lines[i];
					var currentWidth = isOriginalLine[i] ? width : effectiveWidth;

					if (font.Measure(line).X <= currentWidth)
						continue;

					void MarkWrappedContinuationInserted()
					{
						var newIsOriginalLine = new bool[lines.Count];
						for (var j = 0; j <= i; j++)
							newIsOriginalLine[j] = isOriginalLine[j];

						newIsOriginalLine[i + 1] = false;
						for (var j = i + 2; j < lines.Count; j++)
							newIsOriginalLine[j] = isOriginalLine[j - 1];

						isOriginalLine = newIsOriginalLine;
					}

					if (isCjk)
					{
						var breakIndex = WidgetUtils.FindCjkAwareWrapExclusiveEnd(line, currentWidth, font);
						lines[i] = line[..breakIndex].TrimEnd();
						lines.Insert(i + 1, line[breakIndex..].TrimStart());
						MarkWrappedContinuationInserted();
					}
					else
					{
						var (lineEndExclusive, continuationStart) = WidgetUtils.FindLatinWordWrapSplit(line, currentWidth, font);
						if (lineEndExclusive > 0)
						{
							lines[i] = line[..lineEndExclusive];
							lines.Insert(i + 1, line[continuationStart..]);
							MarkWrappedContinuationInserted();
						}
					}
				}

				// Apply indentation only to wrapped lines (not original lines)
				if (indent > 0)
				{
					for (var i = 0; i < lines.Count; i++)
					{
						if (!isOriginalLine[i])
						{
							lines[i] = indentString + lines[i];
						}
					}
				}

				return string.Join("\n", lines);
			}

			return text;
		}
	}
}
