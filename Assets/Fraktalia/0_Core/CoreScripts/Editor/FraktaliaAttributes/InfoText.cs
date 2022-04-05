using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fraktalia.Core.FraktaliaAttributes
{
	public class InfoContent
	{
		public InfoSection Title;
		public List<InfoSection> content;
		public InfoSection section;
		public string videoURL;
		public string videoURL2;
		public string videoURL2Text;
		public string videoURL3;
		public string videoURL3Text;
		public bool showvideo;
	}


	public class BeginInfoAttribute : PropertyAttribute
	{
		public static Dictionary<string, InfoContent> InfoContentDictionary = new Dictionary<string, InfoContent>();
		public static InfoContent CurrentInfoContent;

		public BeginInfoAttribute(string Key)
		{
			if (InfoContentDictionary == null) InfoContentDictionary = new Dictionary<string, InfoContent>();
			InfoContentDictionary[Key] = new InfoContent();
			CurrentInfoContent = InfoContentDictionary[Key];
			CurrentInfoContent.content = new List<InfoSection>();
			CurrentInfoContent.videoURL = "";
			CurrentInfoContent.videoURL2 = "";
			CurrentInfoContent.videoURL3 = "";
			CurrentInfoContent.showvideo = false;
		}
	}

	public class InfoTitleAttribute : PropertyAttribute
	{
		

		public InfoTitleAttribute(string title, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			BeginInfoAttribute.CurrentInfoContent.Title = new InfoSection();
			BeginInfoAttribute.CurrentInfoContent.Title.Title = title;
			BeginInfoAttribute.CurrentInfoContent.Title.Text = text;
		}
	}

	public class InfoSection1Attribute : PropertyAttribute
	{
		public InfoSection1Attribute(string title, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			InfoSection section = new InfoSection();
			section.Title = title;
			section.Text = text;
			BeginInfoAttribute.CurrentInfoContent.content.Add(section);
		}
	}
	public class InfoSection2Attribute : PropertyAttribute
	{
		public InfoSection2Attribute(string title, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			InfoSection section = new InfoSection();
			section.Title = title;
			section.Text = text;
			BeginInfoAttribute.CurrentInfoContent.content.Add(section);
		}
	}
	public class InfoSection3Attribute : PropertyAttribute
	{
		public InfoSection3Attribute(string title, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			InfoSection section = new InfoSection();
			section.Title = title;
			section.Text = text;
			BeginInfoAttribute.CurrentInfoContent.content.Add(section);
		}
	}
	public class InfoSection4Attribute : PropertyAttribute
	{
		public InfoSection4Attribute(string title, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			InfoSection section = new InfoSection();
			section.Title = title;
			section.Text = text;
			BeginInfoAttribute.CurrentInfoContent.content.Add(section);
		}
	}
	public class InfoSection5Attribute : PropertyAttribute
	{
		public InfoSection5Attribute(string title, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			InfoSection section = new InfoSection();
			section.Title = title;
			section.Text = text;
			BeginInfoAttribute.CurrentInfoContent.content.Add(section);
		}
	}

	public class InfoVideoAttribute : PropertyAttribute
	{
		public InfoVideoAttribute(string url, bool showvideo)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			BeginInfoAttribute.CurrentInfoContent.videoURL = url;
			BeginInfoAttribute.CurrentInfoContent.showvideo = showvideo;
			
		}
	}

	public class InfoVideo2Attribute : PropertyAttribute
	{
		public InfoVideo2Attribute(string url, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			BeginInfoAttribute.CurrentInfoContent.videoURL2 = url;
			BeginInfoAttribute.CurrentInfoContent.videoURL2Text = text;

		}
	}

	public class InfoVideo3Attribute : PropertyAttribute
	{
		public InfoVideo3Attribute(string url, string text)
		{
			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			BeginInfoAttribute.CurrentInfoContent.videoURL3 = url;
			BeginInfoAttribute.CurrentInfoContent.videoURL3Text = text;

		}
	}

	public class InfoTextAttribute : PropertyAttribute
	{
		public string labeltext;
		public string Key;

		public InfoTextAttribute(string labeltext, string key)
		{
			this.labeltext = labeltext;
			this.Key = key;
		}
	}

	public struct InfoSection
	{
		public string Title;
		public string Text;
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InfoTextAttribute))]
	public class InfoTextDrawer : DecoratorDrawer
	{


		public override float GetHeight()
		{
			
			return base.GetHeight() + 20;

		}

		// Draw the property inside the given rect
		public override void OnGUI(Rect position)
		{
			// First get the attribute since it contains the range for the slider
			InfoTextAttribute labeltext = attribute as InfoTextAttribute;
			
			GUIStyle style = new GUIStyle();
			style.alignment = TextAnchor.MiddleLeft;
			style.fontStyle = FontStyle.Bold;
			style.fontSize = 14;

			Rect pos = position;

			pos.xMax = position.xMax;
			pos.xMin = position.xMax - 65;
			pos.yMin = position.yMin + 5;
			pos.yMax = position.yMax - 5;

			if (BeginInfoAttribute.CurrentInfoContent == null) return;
			if (GUI.Button(pos, "Info/Help"))
			{
				InfoContent tutorial = BeginInfoAttribute.InfoContentDictionary[labeltext.Key];
				TutorialWindow.Init(tutorial);
			}

		
			EditorGUI.TextField(position, labeltext.labeltext, style);

			

			

		}
	}

#endif


}

