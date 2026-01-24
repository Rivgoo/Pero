import { AnalysisRequest, AnalysisResponse } from '../../shared/contracts';

type DotnetExports = {
  Pero: {
    WasmHost: {
      Engine: {
        Process: (json: string) => string;
      };
    };
  };
};

/**
 * Manages the WASM runtime and provides a clean API for text analysis.
 */
export class AnalysisService {
  private dotnet: DotnetExports | null = null;
  private initializationPromise: Promise<void> | null = null;

  init(): Promise<void> {
    if (!this.initializationPromise) {
      this.initializationPromise = this.loadRuntime();
    }
    return this.initializationPromise;
  }

  private async loadRuntime(): Promise<void> {
    try {
      const dotnetUrl = chrome.runtime.getURL('wasm/dotnet.js');
      
      // @ts-ignore
      const { dotnet } = await import(/* @vite-ignore */ dotnetUrl);
      
      const { getAssemblyExports, getConfig } = await dotnet
        .withDiagnosticTracing(false)
        .create();

      const config = getConfig();
      this.dotnet = await getAssemblyExports(config.mainAssemblyName);

      console.log('Pero: C# WASM Engine Initialized');
    } catch (e) {
      console.error('Pero: Failed to load WASM runtime', e);
      throw e;
    }
  }

  async analyze(text: string): Promise<AnalysisResponse> {
    await this.init();

    if (!this.dotnet) {
      console.error('Pero: WASM runtime not initialized');
      return { requestId: '', isSuccess: false, issues: [] };
    }

    const request: AnalysisRequest = {
      requestId: crypto.randomUUID(),
      text,
      languageCode: 'uk-UA'
    };

    const jsonResponse = this.dotnet.Pero.WasmHost.Engine.Process(JSON.stringify(request));
    
    return JSON.parse(jsonResponse);
  }
}