import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    server: {
        port: 63371,
        proxy: {
            // Target HTTPS directly; the API redirects HTTP to HTTPS, which the browser would block as cross-origin.
            '/api': {
                target: 'https://localhost:7286',
                secure: false,
            },
        },
    }
})