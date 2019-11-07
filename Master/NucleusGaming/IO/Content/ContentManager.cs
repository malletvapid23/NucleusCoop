using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Nucleus.Gaming
{
    public class ContentManager : IDisposable
    {
        private Dictionary<string, Image> loadedImages;
        private bool isDisposed;
        private GenericGameInfo game;
        private string scriptsFolder;
        private string pkgFolder;

        public ContentManager(GenericGameInfo game)
        {
            this.game = game;
            loadedImages = new Dictionary<string, Image>();

            scriptsFolder = GameManager.Instance.GetJsScriptsPath();
            pkgFolder = Path.Combine(scriptsFolder, Path.GetFileNameWithoutExtension(game.JsFileName));
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            foreach (Image image in loadedImages.Values)
            {
                image.Dispose();
            }
            loadedImages = null;
        }

        public Image LoadImage(string url)
        {
            // clear the url
            url = url.ToLower();
            Image img;
            if (loadedImages.TryGetValue(url, out img))
            {
                return img;
            }

            string fullPath = Path.Combine(pkgFolder, url);
            img = Image.FromFile(fullPath);
            loadedImages.Add(url, img);
            return img;
        }
    }
}
