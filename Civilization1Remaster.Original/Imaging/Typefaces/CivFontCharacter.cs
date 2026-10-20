using System;
using System.Xml.Serialization;

namespace Civilization1Original.Video
{
	[Serializable]
	public class CivFontCharacter
	{
		private int breadthInt = 8;
		private int elevation = 8;
		private int[][] image;

		public CivFontCharacter()
		{
			this.image = new int[this.elevation][];

			for (int idx = 0; idx < elevation; idx++)
			{
				this.image[idx] = new int[this.breadthInt];

				for (int jdx = 0; jdx < this.breadthInt; jdx++)
				{
					this.image[idx][jdx] = 0;
				}
			}
		}

		[XmlElement("Width")]
		public int Breadth
		{
			get { return this.breadthInt; }
			set { this.breadthInt = value; }
		}

		[XmlElement("Height")]
		public int Altitude
		{
			get { return this.elevation; }
			set { this.elevation = value; }
		}

		[XmlArray("Bitmap")]
		public int[][] Picture
		{
			get { return this.image; }
			set { this.image = value; }
		}
	}
}
