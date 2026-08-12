export interface ApiErrorResponse {
  statusCode: number
  message: string
  errors?: { field: string; error: string }[]
}
