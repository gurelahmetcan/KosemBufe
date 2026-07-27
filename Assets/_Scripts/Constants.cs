namespace Bufe
{
    public static class Constants
    {
        #region Item Types

        public enum ItemCategory  { Premade, Prepared }

        public enum IngredientType
        {
            None,
            Bread_Full,
            Bread_Half,
            Sucuk,
            Cheese,
            Ayran,
            Cips,
            Tea,
            Cigarette,
            Cola
        }

        #endregion

        #region Order Status

        public enum CookState     { Raw, Cooked}
        public enum OrderResult   { Perfect, WrongIngredients, Raw}

        #endregion

    }
}

