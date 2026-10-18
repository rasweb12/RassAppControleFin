abstract final class AppConfig {
  static const _apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://rassappcontrolefin-1.onrender.com',
  );

  static final Uri apiBaseUri = Uri.parse(
    _apiBaseUrl,
  );
  static const requestTimeout = Duration(seconds: 20);
}
