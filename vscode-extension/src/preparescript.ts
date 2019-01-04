let ncp = require('ncp').ncp;
let path = require('path');
let fs = require('fs');

// NOTE: concurrency limit
ncp.limit = 16;

// TODO: debug vs release
const sourcePath = path.normalize(path.join(__dirname, "..", "..", "src/Avalonia.Ide.LanguageServer/bin/Release/netcoreapp2.1"));
const destinationPath = path.normalize(path.join(__dirname, "..", "lsp_binaries"));

if (!fs.existsSync(destinationPath)) {

  ncp(sourcePath, destinationPath, function (err) {
    if (err) {
      return console.error(err);
    }
    console.log(`Restoring 'lsp_binaries' completed`);
  });

}
