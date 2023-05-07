using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SysIO = System.IO;

namespace MUpdater.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class UpdateController : ControllerBase
    {
        IConfiguration config;
        public UpdateController(IConfiguration pConfiguration)
        {
            config = pConfiguration;
            if (config["RepoPath"] is string path)
            {
                base_path = path;
            }
            if (!SysIO.Directory.Exists(base_path))
            {
                SysIO.Directory.CreateDirectory(base_path);
            }
        }
        private string base_path = "repo";



        [HttpPost("{pScope}/Info")]
        public IActionResult LatestFileInfo(string pScope, [FromBody] List<string> pFiles)
        {
            string path = $"{base_path}/{pScope}/";
            if (!SysIO.Directory.Exists(path)) return NotFound();


            var data = Directory.EnumerateFiles(path, "*.*", new EnumerationOptions() { RecurseSubdirectories = true })
                    .Select(x => x)
                    .Where(x => pFiles.Count == 0 | (pFiles.Count > 0 && pFiles.Any(y => x.Replace("\\", "/").Replace(path, "") == y.Replace("\\", "/"))))
                    .Select(x =>
                       {
                           var info = new FileInfo(x);
                           return new
                           {
                               FileName = x.Replace("\\", "/").Replace(path, ""),
                               info.LastWriteTime,
                               info.LastAccessTime
                           };

                       });

            return Content(System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions() { ReferenceHandler = ReferenceHandler.IgnoreCycles }), "application/json");
        }

        [HttpPost("{pScope}/Fetch")]
        public async Task<IActionResult> FetchFilesAsync(string pScope, [FromBody] List<string> pFiles)
        {
            string path = $"{base_path}/{pScope}/";
            if (!SysIO.Directory.Exists(path)) return NotFound();

            var files = Directory.EnumerateFiles(path, "*.*", new EnumerationOptions() { RecurseSubdirectories = true })
                    .Where(x => pFiles.Count == 0 | (pFiles.Count > 0 && pFiles.Any(y => x.Replace("\\", "/").Replace(path, "") == y.Replace("\\", "/")))).ToList()
                    .Select(x => new FileInfo(x).FullName);


            return await ZipedUpdateResult(pScope, files);

        }

        public async Task<IActionResult> ZipedUpdateResult(string pScope, IEnumerable<string> pFiles)
        {
            string path = $"{base_path}/{pScope}";
            using (var zip_stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zip_stream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in pFiles)
                    {
                        // Add the file to the zip archive
                        var name = file.Replace(new DirectoryInfo(path).FullName + "\\", "");
                        var entry = archive.CreateEntry(name + ".mbooks");

                        using (var entry_stream = entry.Open())
                        {
                            using var ms = new MemoryStream(SysIO.File.ReadAllBytes(file));
                            await ms.CopyToAsync(entry_stream);
                        }
                    }
                }

                var zip_file = $"{pScope}.zip";
                return File(zip_stream.ToArray(), "application/zip", zip_file);

            }
        }


    }
}