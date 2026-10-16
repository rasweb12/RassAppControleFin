class AuthSession {
  static const renewalLeadTime = Duration(minutes: 1);
  const AuthSession({
    required this.sessionId,
    required this.accessToken,
    required this.sessionToken,
    required this.subject,
    required this.accessExpiresAt,
    required this.sessionExpiresAt,
  });
  final String sessionId;
  final String accessToken;
  final String sessionToken;
  final String subject;
  final DateTime accessExpiresAt;
  final DateTime sessionExpiresAt;

  bool get isSessionExpired =>
      !sessionExpiresAt.isAfter(DateTime.now().toUtc());
  bool get isAccessExpiring =>
      !accessExpiresAt.isAfter(DateTime.now().toUtc().add(renewalLeadTime));

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
    sessionId: json['sessionId'] as String,
    accessToken: json['accessToken'] as String,
    sessionToken: json['sessionToken'] as String,
    subject: json['subject'] as String,
    accessExpiresAt: DateTime.parse(json['accessExpiresAt'] as String).toUtc(),
    sessionExpiresAt: DateTime.parse(
      json['sessionExpiresAt'] as String,
    ).toUtc(),
  );

  Map<String, dynamic> toJson() => {
    'sessionId': sessionId,
    'accessToken': accessToken,
    'sessionToken': sessionToken,
    'subject': subject,
    'accessExpiresAt': accessExpiresAt.toIso8601String(),
    'sessionExpiresAt': sessionExpiresAt.toIso8601String(),
  };
}
