export const ehCatalogo = (plataforma) => Boolean(
  plataforma.porAssinatura && !plataforma.compraDigital &&
  !plataforma.midiaFisica && !plataforma.resgatadoDeAssinatura
);

export const ehJogoDaBiblioteca = (jogo) =>
  (jogo.plataformas ?? []).some((plataforma) => !ehCatalogo(plataforma));
