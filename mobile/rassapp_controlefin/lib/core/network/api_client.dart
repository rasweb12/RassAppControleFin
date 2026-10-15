import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/app_config.dart';

enum ApiFailure {
  validation,
  conflict,
  unauthorized,
  forbidden,
  unavailable,
  unexpected,
}

class ApiException implements Exception {
  const ApiException(this.failure, this.message);
  final ApiFailure failure;
  final String message;
}

class ApiClient {
  ApiClient({http.Client? client}) : _client = client ?? http.Client();
  final http.Client _client;

  Future<Map<String, dynamic>> post(
    String path,
    Map<String, dynamic> payload, {
    Map<String, String> headers = const {},
  }) async {
    final data = await _send(
      () => _client.post(
        AppConfig.apiBaseUri.resolve(path),
        headers: _headers(headers),
        body: jsonEncode(payload),
      ),
    );
    if (data is Map<String, dynamic>) return data;
    throw const ApiException(
      ApiFailure.unexpected,
      'A resposta recebida não possui o formato esperado.',
    );
  }

  Future<Object?> get(
    String path, {
    Map<String, String> headers = const {},
    Map<String, String>? queryParameters,
  }) => _send(
    () => _client.get(
      AppConfig.apiBaseUri
          .resolve(path)
          .replace(queryParameters: queryParameters),
      headers: _headers(headers),
    ),
  );

  Map<String, String> _headers(Map<String, String> additional) => {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
    ...additional,
  };

  Future<Object?> _send(Future<http.Response> Function() request) async {
    try {
      final response = await request().timeout(AppConfig.requestTimeout);
      final body = response.body.isEmpty
          ? <String, dynamic>{}
          : jsonDecode(response.body) as Map<String, dynamic>;
      if (response.statusCode >= 200 &&
          response.statusCode < 300 &&
          body['success'] == true) {
        return body['data'];
      }
      final error = body['error'] as Map<String, dynamic>?;
      final failure = switch (response.statusCode) {
        400 => ApiFailure.validation,
        409 => ApiFailure.conflict,
        401 => ApiFailure.unauthorized,
        403 => ApiFailure.forbidden,
        429 => ApiFailure.unavailable,
        503 => ApiFailure.unavailable,
        _ => ApiFailure.unexpected,
      };
      throw ApiException(
        failure,
        error?['message'] as String? ??
            'Não foi possível concluir a solicitação.',
      );
    } on ApiException {
      rethrow;
    } on Exception {
      throw const ApiException(
        ApiFailure.unavailable,
        'Autenticação indisponível. Tente novamente mais tarde.',
      );
    }
  }
}
