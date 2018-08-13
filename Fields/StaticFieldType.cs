using System.Collections.Generic;
using System.Web.UI;

using Rock;
using Rock.Field;
using Rock.Web.UI.Controls;

namespace com.shepherdchurch.Misc.Fields
{
    public class StaticFieldType : FieldType
    {
        #region Configuration

        /// <summary>
        /// Static Value Key.
        /// </summary>
        protected const string STATIC_VALUE_KEY = "StaticValue";

        /// <summary>
        /// Returns a list of the configuration keys
        /// </summary>
        /// <returns></returns>
        public override List<string> ConfigurationKeys()
        {
            List<string> configKeys = new List<string>
            {
                STATIC_VALUE_KEY
            };

            return configKeys;
        }

        /// <summary>
        /// Creates the HTML controls required to configure this type of field
        /// </summary>
        /// <returns></returns>
        public override List<Control> ConfigurationControls()
        {
            List<Control> controls = new List<Control>();

            var codeEditor = new CodeEditor();
            codeEditor.EditorTheme = CodeEditorTheme.Rock;
            codeEditor.EditorMode = CodeEditorMode.Html;

            controls.Add( codeEditor );

            return controls;
        }

        /// <summary>
        /// Gets the configuration value.
        /// </summary>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override Dictionary<string, ConfigurationValue> ConfigurationValues( List<Control> controls )
        {
            Dictionary<string, ConfigurationValue> configurationValues = new Dictionary<string, ConfigurationValue>
            {
                { STATIC_VALUE_KEY, new ConfigurationValue( "Static Value", "Static Value to Display", "" ) }
            };

            if ( controls != null && controls.Count == 1 )
            {
                if ( controls[0] != null && controls[0] is CodeEditor )
                {
                    configurationValues[STATIC_VALUE_KEY].Value = ( ( CodeEditor ) controls[0] ).Text;
                }
            }

            return configurationValues;
        }

        /// <summary>
        /// Sets the configuration value.
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="configurationValues"></param>
        public override void SetConfigurationValues( List<Control> controls, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( controls != null && controls.Count == 1 && configurationValues != null )
            {
                if ( controls[0] != null && controls[0] is CodeEditor && configurationValues.ContainsKey( STATIC_VALUE_KEY ) )
                {
                    ( ( CodeEditor ) controls[0] ).Text = configurationValues[STATIC_VALUE_KEY].Value;
                }
            }
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Returns the field's current value(s)
        /// </summary>
        /// <param name="parentControl">The parent control.</param>
        /// <param name="value">Information about the value</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="condensed">Flag indicating if the value should be condensed (i.e. for use in a grid column)</param>
        /// <returns></returns>
        public override string FormatValue( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            return string.Empty;
        }

        #endregion

        #region Edit Control

        /// <summary>
        /// Creates the control(s) necessary for prompting user for a new value
        /// </summary>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="id"></param>
        /// <returns>
        /// The control
        /// </returns>
        public override Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            if ( configurationValues != null )
            {
                string staticValue = string.Empty;

                if ( configurationValues.ContainsKey( STATIC_VALUE_KEY ) )
                {
                    staticValue = configurationValues[STATIC_VALUE_KEY].Value;
                }

                var control = new com.shepherdchurch.Misc.UI.Controls.StaticRockControl
                {
                    ID = id,
                    Text = staticValue
                };

                return control;
            }

            return null;
        }

        /// <summary>
        /// Reads new values entered by the user for the field
        /// return value as Category.Guid
        /// </summary>
        /// <param name="control">Parent control that controls were added to in the CreateEditControl() method</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <returns></returns>
        public override string GetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            return string.Empty;
        }

        /// <summary>
        /// Sets the value.
        /// value is a Category.Guid string
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="configurationValues">The configuration values.</param>
        /// <param name="value">The value.</param>
        public override void SetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
        }

        #endregion
    }
}
