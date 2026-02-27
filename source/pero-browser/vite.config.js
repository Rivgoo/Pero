import { defineConfig } from 'vite';
import { resolve } from 'path';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': resolve(__dirname, './src'),
      '@app': resolve(__dirname, './src/app'),
      '@shared': resolve(__dirname, './src/shared'),
      '@core': resolve(__dirname, './src/core'),
      '@content': resolve(__dirname, './src/content')
    }
  },
  build: {
    rollupOptions: {
      external: ['/wasm/dotnet.js'],
      input: {
        background: resolve(__dirname, 'src/background/index.ts'),
        content: resolve(__dirname, 'src/content/index.ts'),
        offscreen: resolve(__dirname, 'src/offscreen/index.html'),
        popup: resolve(__dirname, 'src/popup/index.html'),
        app: resolve(__dirname, 'src/app/index.html')
      },
      output: {
        entryFileNames: '[name].js',
        chunkFileNames: 'chunks/[name]-[hash].js',
        assetFileNames: (assetInfo) => {
          const name = assetInfo.name || '';
          
          if (name === 'content.css' || name === 'styles.css' || name === 'session.css') {
            return 'content.css';
          }
          
          return 'assets/[name]-[hash].[ext]';
        },
        format: 'es'
      }
    },
    outDir: 'dist',
    emptyOutDir: true
  }
});