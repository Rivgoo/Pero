import { AnalysisRequest, AnalysisResponse } from '../../shared/contracts';
import { DebugLogger } from './DebugLogger';

interface DotnetEngine {
  readonly Process: (json: string) => string;
}

interface DotnetExports {
  readonly Pero: {
    readonly WasmHost: {
      readonly Engine: DotnetEngine;
    };
  };
}

export class WasmLoader {
  private static instance: WasmLoader;
  private dotnetExports: DotnetExports | null = null;
  private initPromise: Promise<void> | null = null;
  private readonly logger: DebugLogger;

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

  analyze(request: AnalysisRequest): AnalysisResponse {
    if (!this.dotnetExports) {
      throw new Error('WASM Runtime not initialized.');
    }

    const startMs = performance.now();

    try {
      const jsonIn = JSON.stringify(request);
      
      if (request.debug) {
        this.logger.logRequest(jsonIn);
      }

      const jsonOut = this.dotnetExports.Pero.WasmHost.Engine.Process(jsonIn);
      const endMs = performance.now();

      if (request.debug) {
        this.logger.logResponse(jsonOut, endMs - startMs);
      }

      return JSON.parse(jsonOut);
    } catch (error) {
      console.error('Pero: Analysis Execution Failed', error);
      return this.createErrorResponse(request.requestId);
    }
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
    } catch (error) {
      this.initPromise = null;
      throw new Error(`Pero: WASM Load Failed - ${(error as Error).message}`);
    }
  }

  private createErrorResponse(requestId: string): AnalysisResponse {
    return {
      requestId,
      isSuccess: false,
      issues: []
    };
  }
}