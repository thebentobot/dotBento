namespace dotBento.Domain.Enums;

public enum CommandResponse
{
    Ok = 1,
    WrongInput = 2,
    NotFound = 3,
    NotSupportedInDm = 4,
    Error = 5,
    NoPermission = 6,
    Cooldown = 7,
    RateLimited = 8,
    OnlySupportedInDm = 9,
    Help = 10,
}