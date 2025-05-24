mergeInto(LibraryManager.library, {
  DownloadFile: function(fileNamePtr, dataPtr, length) {
    var fileName = UTF8ToString(fileNamePtr);
    var data = new Uint8Array(Module.HEAPU8.buffer, dataPtr, length);
    var blob = new Blob([data], { type: "application/octet-stream" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    setTimeout(function() {
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    }, 100);
  }
}); 