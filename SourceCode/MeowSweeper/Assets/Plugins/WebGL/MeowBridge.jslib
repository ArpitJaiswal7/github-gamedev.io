mergeInto(LibraryManager.library, {
  SyncGridSizeToJS: function(size) {
    if (typeof window !== 'undefined') {
      window.currentMeowGridSize = size;
      console.log("[MeowBridge] Synced grid size to JS: " + size);
    }
  }
});
