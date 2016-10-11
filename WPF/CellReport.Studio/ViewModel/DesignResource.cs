using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

using CellReport.Core;

namespace CellReport.Studio.ViewModel
{
	public class DesignResource : NotifyPropertyChanged
	{
		public DesignResource(string name, byte[] data)
		{
			Name = name;
			Data = data;
		}

		public string Name
		{
			get { return GetParam<string>("Name"); }
			set { SetParam("Name", value); }
		}

		public byte[] Data
		{
			get { return GetParam<byte[]>("Data"); }
			set { SetParam("Data", value); SetImage(); }
		}

		public string NewName
		{
			get { return GetParam<string>("NewName"); }
			set { SetParam("NewName", value); }
		}

		public bool IsEditMode
		{
			get { return GetParam<bool>("IsEditMode"); }
			set { SetParam("IsEditMode", value); }
		}

		public BitmapImage Image { get; private set; }

		public void SetImage()
		{
			Image = null;
			if ((Data == null) || (Data.Length == 0))
				return;

			Image = new BitmapImage();
			using (var ms = new MemoryStream(Data))
			{
				Image.BeginInit();
				Image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				Image.CacheOption = BitmapCacheOption.OnLoad;
				Image.StreamSource = ms;
				Image.EndInit();
			}
			Image.Freeze();
		}

		public static DesignResource FromFile(string path, IList<DesignResource> resources)
		{
			if (!File.Exists(path))
				return null;

			var fileName = Path.GetFileNameWithoutExtension(path);
			string resName = null;
			byte[] data = null;
			var i = 0;
			do
			{
				resName = (i > 0) ? fileName + i.ToString() : fileName;
				data = resources.Where(x => x.Name == resName).Select(x => x.Data).FirstOrDefault();
				i++;
			} while ((data != null) && (data.Length > 0));

			data = File.ReadAllBytes(path);
			return new DesignResource(resName, data);
		}

		public static void SaveResources(IList<DesignResource> resources)
		{
			foreach (var name in StudioCommands.Manager.GetResourceNames().ToList())
				StudioCommands.Manager.RemoveResource(name);

			foreach (var res in resources)
				StudioCommands.Manager.AddResource(res.Name, res.Data);
		}
	}
}
