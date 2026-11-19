import { ehCatalogo, ehJogoDaBiblioteca } from './acesso.js';

test('classifica a posse em todas as combinações de flags', () => {
  for (let flags = 0; flags < 16; flags += 1) {
    expect(ehCatalogo({
      compraDigital: Boolean(flags & 1),
      midiaFisica: Boolean(flags & 2),
      porAssinatura: Boolean(flags & 4),
      resgatadoDeAssinatura: Boolean(flags & 8)
    })).toBe(flags === 4);
  }
});

test('um jogo entra na biblioteca quando tem pelo menos uma posse', () => {
  const catalogo = { porAssinatura: true };
  expect(ehJogoDaBiblioteca({ plataformas: [catalogo] })).toBe(false);
  expect(ehJogoDaBiblioteca({ plataformas: [catalogo, { compraDigital: true }] })).toBe(true);
});
