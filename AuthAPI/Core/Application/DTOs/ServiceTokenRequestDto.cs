using System;

namespace AuthAPI.Core.Application.DTOs;

public class ServiceTokenRequestDto
{
  public string ClientId { get; set; } = string.Empty;
  public string ClientSecret { get; set; } = string.Empty;
}
