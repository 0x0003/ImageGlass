import './main';

import Sidebar from './FrmSettings/Sidebar';
import Settings from './FrmSettings/Settings';
import TabAppearance from './FrmSettings/TabAppearance';


if (!window._pageSettings) {
  window._pageSettings = {
    initTab: 'layout',
    config: {},
    langList: [],
    toolList: [],
    themeList: [],
    enums: {
      ImageOrderBy: [],
      ImageOrderType: [],
      ColorProfileOption: [],
      AfterEditAppAction: [],
      ImageInterpolation: [],
      MouseWheelAction: [],
      MouseWheelEvent: [],
      MouseClickEvent: [],
      BackdropStyle: [],
      ToolbarItemModelType: [],
    },
    icons: {
      Delete: '',
      Edit: '',
      Moon: '',
      Sun: '',
    },
    startUpDir: '',
    configDir: '',
    userConfigFilePath: '',
    defaultThemeDir: '',
    FILE_MACRO: '',
  };
}
_page.loadSettings = Settings.load;
_page.loadBackgroundColorConfig = TabAppearance.loadBackgroundColorConfig;


// sidebar
Sidebar.addEvents();

// load the last open tab
Sidebar.setActiveMenu(_pageSettings.initTab);