// Runtime config: edit this file on the deploy target without rebuilding
// the Angular bundle. The Pi-hosted Caddy serves it as /config.js next to
// the static assets; for local dev the value is overridden via the
// development environment file.
window.TC_CONFIG = {
  apiUrl: 'http://localhost:5555/api'
};
