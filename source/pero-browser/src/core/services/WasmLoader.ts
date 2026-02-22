import { AnalysisRequest, AnalysisResponse } from '../../shared/contracts';
import { DebugLogger } from './DebugLogger';

// Internal type definition for the .NET exports
type DotnetExports = {
  Pero: {
    WasmHost: {
      Engine: {
        Process: (json: string) => string;
      };
    };
  };
};

export class WasmLoader {
  private static instance: WasmLoader;
  private dotnetExports: DotnetExports | null = null;
  private initPromise: Promise<void> | null = null;
  private logger: DebugLogger;

  private constructor() {
    this.logger = DebugLogger.getInstance();
  }

  static getInstance(): WasmLoader {
    if (!WasmLoader.instance) {
      WasmLoader.instance = new WasmLoader();
    }
    return WasmLoader.instance;
  }

  async initialize(): Promise<void> {
    if (this.dotnetExports) return;
    
    if (!this.initPromise) {
      this.initPromise = this.loadRuntime();
    }
    return this.initPromise;
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
      this.dotnetExports = await getAssemblyExports(config.mainAssemblyName);
      
      console.log('Pero: WASM Runtime Loaded');
    } catch (error) {
      console.error('Pero: WASM Load Failed', error);
      this.initPromise = null;
      throw error;
    }
  }

  analyze(request: AnalysisRequest): AnalysisResponse {
    if (!this.dotnetExports) {
      throw new Error('Runtime not initialized');
    }

    const start = performance.now();

    try {
      const jsonIn = JSON.stringify(request);
    
      const shouldLog = request.debug === true;

      this.logger.logRequest(jsonIn, shouldLog);

      const jsonOut = this.dotnetExports.Pero.WasmHost.Engine.Process(jsonIn);
      
      const end = performance.now();

      this.logger.logResponse(jsonOut, end - start, shouldLog);

      return JSON.parse(jsonOut);
    } catch (e) {
      console.error('Pero: Analysis Execution Failed', e);
      return {
        requestId: request.requestId,
        isSuccess: false,
        issues: []
      };
    }
  }
}