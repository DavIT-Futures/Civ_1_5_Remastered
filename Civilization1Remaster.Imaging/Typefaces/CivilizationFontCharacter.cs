namespace Civilization1Remaster.Imaging.Typefaces
{
	public class CivilizationFontCharacter
	{
		private int breadthValue = 8;
		private int elevation = 8;
		private int[][] image;

		public CivilizationFontCharacter()
		{
			this.image = new int[this.elevation][];

			for (int index = 0; index < elevation; index++)
			{
				this.image[index] = new int[this.breadthValue];

				for (int innerIndex = 0; innerIndex < this.breadthValue; innerIndex++)
				{
					this.image[index][innerIndex] = 0;
				}
			}
		}

		public int Breadth
		{
			get { return this.breadthValue; }
			set { this.breadthValue = value; }
		}

		public int Altitude
		{
			get { return this.elevation; }
			set { this.elevation = value; }
		}

		public int[][] Picture
		{
			get { return this.image; }
			set { this.image = value; }
		}
	}
}
