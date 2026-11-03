abstract final class AppConfig {
  static const _apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: '',
  );

  // Tests may construct HTTP clients without a real endpoint. This inert URL
  // avoids directing an unconfigured binary to Production.
  static final Uri apiBaseUri = Uri.parse(
    _apiBaseUrl.isEmpty ? 'https://configuration-required.invalid' : _apiBaseUrl,
  );

  static bool get isConfigured =>
      _apiBaseUrl.isNotEmpty && _isAllowedApiUri(apiBaseUri);

  static String? get configurationError {
    if (_apiBaseUrl.isEmpty) {
      return 'Este aplicativo precisa ser compilado com API_BASE_URL.';
    }
    if (!_isAllowedApiUri(apiBaseUri)) {
      return 'API_BASE_URL deve usar HTTPS ou um endpoint local do emulador.';
    }
    return null;
  }

  static bool _isAllowedApiUri(Uri uri) {
    if (!uri.isAbsolute || uri.host.isEmpty || uri.userInfo.isNotEmpty) {
      return false;
    }
    if (uri.scheme == 'https') return true;
    return uri.scheme == 'http' &&
        (uri.host == '10.0.2.2' || uri.host == '127.0.0.1' || uri.host == 'localhost');
  }

  static const requestTimeout = Duration(seconds: 20);
}
