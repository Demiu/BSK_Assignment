using Xunit;

namespace tests;

public class AssymmetricContainerTest 
{
    readonly string pem;

    public AssymmetricContainerTest() {
        pem = @"-----BEGIN RSA PUBLIC KEY-----
MIIBigKCAYEAotUIAS/Q4qOtDB4tu/N4YG37HZ55FVxrSdtnGME92u5NA2tCO/eO
KlTx0bGhRKTGW4F7LaVbhLrgCrbJIVqgtJwVhJsfSL1dEcTUBCeaL96Hl97psOEo
PYX9y8UpC1kZwJc/EW58rXxdvMV77YltBIkkG9olEtMm+oGZkeRRtH733W6s8y76
QG+UfOzm3TZRFKjQoxszbSwvQhp/qtuNk+0JWlgAoQ9BEiE1y5cp6+11dJJ+RInG
HMwW6rmuRJ3dHGmAd/Y2qW795PKZRRQNslbut9EXxTvYF8xIrAMNVLZB28U2c44R
8N/YlS1RuZ43O1kBANFUrPysW96EwzYP+YJocR5iHWezTN1Vqqe63mjec/PdzWBQ
B1z1nWVaFeaq87vfPKKeU30sBllK3GJIJwHGtUF1GHB1QpMcAeKa+bPB2vPCX0o2
cAaEN9CQwn+cQpkLryuGogZBYBRyS9ne7jjO+RlMbRrAcxrMdkhUWEevhfErBnPC
qf0bX1MvZiiPAgMBAAE=
-----END RSA PUBLIC KEY-----";
    }

    [Fact]
    public void LoadPubFromPemGoodRsaKey() {
        var container = new Lib.Crypto.AsymmetricContainer();
        Assert.True(container.LoadPubFromPem(pem));
    }

    [Fact]
    public void LoadPubFromPemBadRsaKey() {
        var wrongPem = pem.Replace("RSA PUBLIC KEY", "MUMBO JUMBO");
        var container = new Lib.Crypto.AsymmetricContainer();
        Assert.False(container.LoadPubFromPem(wrongPem));
    }
}