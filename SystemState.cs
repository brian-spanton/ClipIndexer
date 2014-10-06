using foobalator;

namespace ClipIndexer
{
    public static class SystemState
    {
        #region Data

        private static ISettings m_Settings;

        #endregion

        #region Properties

        public static ISettings Settings
        {
            get
            {
                return m_Settings;
            }
            set
            {
                m_Settings = value;
            }
        }

        #endregion
    }
}
