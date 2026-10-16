import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../config/app_config.dart';

enum ApiFailure {
  validation,
  unauthorized,
  forbidden,
  notFound,
  conflict,
  ruleViolation,
  rateLimited,
  timeout,
  offline,
  unavailable,
  unexpected,
}

class ApiException implements Exception {
  const ApiException(
    this.failure,
    this.message, {
    this.statusCode,
    this.correlationId,
  });
  final ApiFailure failure;
  final String message;
  final int? statusCode;
  final String? correlationId;
}

class ApiClient {
  ApiClient({http.Client? client}) : _client = client ?? http.Client();
  final http.Client _client;

  Future<Map<String, dynamic>> post(
    String path,
    Map<String, dynamic> payload, {
    Map<String, String> headers = const {},
  }) async {
    final data = await postData(path, payload, headers: headers);
    if (data is Map<String, dynamic>) return data;
    throw const ApiException(
      ApiFailure.unexpected,
      'A resposta recebida não possui o formato esperado.',
    );
  }

  Future<Object?> postData(
    String path,
    Object? payload, {
    Map<String, String> headers = const {},
  }) => _send(
    () => _client.post(
      AppConfig.apiBaseUri.resolve(path),
      headers: _headers(headers),
      body: jsonEncode(payload),
    ),
  );

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
      final correlationId = response.headers['x-correlation-id'];
      if (response.body.isEmpty) throw const FormatException('Empty response.');
      final decoded = jsonDecode(response.body);
      if (decoded is! Map<String, dynamic>) {
        throw const FormatException('Invalid envelope.');
      }
      final body = decoded;
      if (response.statusCode >= 200 &&
          response.statusCode < 300 &&
          body['success'] == true) {
        return body['data'];
      }
      final error = body['error'];
      final failure = switch (response.statusCode) {
        400 => ApiFailure.validation,
        401 => ApiFailure.unauthorized,
        403 => ApiFailure.forbidden,
        404 => ApiFailure.notFound,
        409 => ApiFailure.conflict,
        422 => ApiFailure.ruleViolation,
        429 => ApiFailure.rateLimited,
        500 => ApiFailure.unavailable,
        503 => ApiFailure.unavailable,
        _ => ApiFailure.unexpected,
      };
      throw ApiException(
        failure,
        error is Map && error['message'] is String
            ? error['message'] as String
            : _defaultMessage(response.statusCode),
        statusCode: response.statusCode,
        correlationId: correlationId,
      );
    } on ApiException {
      rethrow;
    } on TimeoutException {
      throw const ApiException(
        ApiFailure.timeout,
        'O tempo de resposta foi excedido. Tente novamente.',
      );
    } on SocketException {
      throw const ApiException(
        ApiFailure.offline,
        'Sem conexão com a internet.',
      );
    } on FormatException {
      throw const ApiException(
        ApiFailure.unexpected,
        'A resposta do serviço não possui o formato esperado.',
      );
    } on Exception {
      throw const ApiException(
        ApiFailure.unavailable,
        'Não foi possível concluir a solicitação agora.',
      );
    }
  }

  String _defaultMessage(int statusCode) => switch (statusCode) {
    400 => 'Revise os dados informados.',
    401 => 'Sua sessão não é mais válida.',
    403 => 'Você não possui permissão para esta operação.',
    404 => 'O recurso solicitado não foi encontrado.',
    409 => 'Os dados foram alterados. Atualize e tente novamente.',
    422 => 'Esta operação não atende às regras financeiras.',
    429 => 'Muitas solicitações. Aguarde um instante.',
    500 || 503 => 'O serviço está temporariamente indisponível.',
    _ => 'Não foi possível concluir a solicitação.',
  };
}
