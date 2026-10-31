mergeInto(LibraryManager.library, {
  RoyaleState: function(ptr) {
    if (window.updateRoyaleState) window.updateRoyaleState(JSON.parse(UTF8ToString(ptr)));
  }
});
