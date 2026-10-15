abstract final class AppConfig {
  static final Uri apiBaseUri = Uri.parse(
    'https://rassappcontrolefin-1.onrender.com',
  );
  static const requestTimeout = Duration(seconds: 20);
}
