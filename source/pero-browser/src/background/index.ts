let creating: Promise<void> | null = null;

async function setupOffscreenDocument(path: string) {
  // Check if an offscreen document already exists
  const existingContexts = await chrome.runtime.getContexts({
    contextTypes: [chrome.runtime.ContextType.OFFSCREEN_DOCUMENT],
  });

  if (existingContexts.length > 0) {
    return;
  }

  // Create one if not
  if (creating) {
    await creating;
  } else {
    creating = chrome.offscreen.createDocument({
      url: path,
      reasons: [chrome.offscreen.Reason.WORKERS],
      justification: 'Hosting .NET WASM runtime for text analysis',
    });
    await creating;
    creating = null;
  }
}

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message.type === 'CHECK_TEXT') {
    
    setupOffscreenDocument('src/offscreen/index.html')
      .then(() => {
        return chrome.runtime.sendMessage(message);
      })
      .then(sendResponse)
      .catch((err) => {
        console.error('Pero: Background error', err);
        sendResponse({ isSuccess: false, issues: [] });
      });

    return true; // Keep channel open
  }
  return false;
});

console.log('Pero: Background Manager Ready');