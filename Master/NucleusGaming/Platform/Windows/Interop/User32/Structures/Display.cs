using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Nucleus
{
    public class Display
    {
        public Rectangle Bounds
        {
            get { return bounds; }
        }
        public string DeviceName
        {
            get { return deviceName; }
        }
        public bool Primary
        {
            get { return primary; }
        }
        public IntPtr Handle
        {
            get { return ptr; }
        }
        public string DeviceID
        {
            get { return deviceID; }
        }
        public string DeviceString
        {
            get { return deviceString; }
        }
        public string MonitorID
        {
            get { return monitorID; }
        }
        public int DisplayIndex
        {
            get { return displayIndex; }
        }
        public int MonitorIndex
        {
            get { return monitorIndex; }
        }

        private Rectangle bounds;
        private string deviceName;
        private string deviceID;
        private string deviceString;
        private string monitorID;
        private int displayIndex;
        private int monitorIndex;
        private bool primary;
        private IntPtr ptr;

        public Display(IntPtr pointer, Rectangle size, string device, bool isPrimary, string devID, string devStr, string monID, int disIndex, int monIndex)
        {
            ptr = pointer;
            bounds = size;
            deviceName = device;
            primary = isPrimary;
            deviceID = devID;
            deviceString = devStr;
            monitorID = monID;
            displayIndex = disIndex;
            monitorIndex = monIndex;
        }

    }
}
