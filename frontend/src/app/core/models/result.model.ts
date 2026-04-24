/**
 * Mirror of the backend Result pattern. Handlers return one of the
 * non-success variants alongside a message; the 400 body carries failures
 * from FluentValidation or the booking rule engine.
 */
export type ResultStatus =
  | 'Success'
  | 'Unauthorized'
  | 'Invalid'
  | 'NotFound'
  | 'Conflict';

export interface ValidationFailure {
  code: string;
  message: string;
}

export interface ApiError {
  error?: string;
  failures?: ValidationFailure[];
}
